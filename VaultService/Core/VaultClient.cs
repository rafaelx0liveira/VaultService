using Serilog;
using System.Collections.Concurrent;
using VaultService.Exceptions;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace VaultService.Core
{
    public class VaultClient : Interface.IVaultClient
    {
        private IVaultClient _vaultClient;
        private readonly ConcurrentDictionary<string, string> _secrets = new();
        private readonly ILogger _logger;
        private string _mountPoint;
        private string _basePath;

        public VaultClient()
        {
            _logger = Log.ForContext<VaultClient>();
        }

        /// <summary>
        /// Configures the connection with Vault.
        /// </summary>
        /// <param name="vaultAddress">Vault URL</param>
        /// <param name="vaultToken">Authentication token</param>
        /// <param name="mountPoint">Vault MountPoint (default: "secret")</param>
        /// <param name="basePath">Base path for secrets</param>
        public void Connect(string vaultAddress, string vaultToken, string mountPoint = "secret", string basePath = "")
        {
            if (string.IsNullOrWhiteSpace(vaultAddress))
            {
                _logger.Fatal("[VaultService] - Vault address is null or empty");
                throw new ArgumentNullException(nameof(vaultAddress), "Vault address cannot be null");
            }

            if (string.IsNullOrWhiteSpace(vaultToken))
            {
                _logger.Fatal("[VaultService] - Vault token is null or empty");
                throw new ArgumentNullException(nameof(vaultToken), "Vault token cannot be null"); 
            }

            _logger.Information("[VaultService] - Initializing connection to Vault at {VaultAddress}", vaultAddress);
            try
            {
                var authMethod = new TokenAuthMethodInfo(vaultToken);
                var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod);
                _vaultClient = new VaultSharp.VaultClient(vaultClientSettings);

                _mountPoint = mountPoint;
                _basePath = basePath;
                var healthStatus = _vaultClient.V1.System.GetHealthStatusAsync().Result;
                if (!healthStatus.Sealed)
                {
                    _logger.Information($"[VaultService] - Successfully connected to Vault at {vaultAddress} with mount point '{mountPoint}'");
                }
                else
                {
                    throw new VaultClientException($"Vault at {vaultAddress} is sealed and cannot be accessed.");
                }
            }
            catch (Exception ex)
            {
                _logger.Fatal($"[VaultService] - Failed to connect to Vault at {vaultAddress}. Error: {ex}");
                throw new VaultClientException($"Failed to connect to Vault at {vaultAddress}. Error: {ex}");
            }
        }

        /// <summary>
        /// Retrieves a secret from Vault in the format "path:key"
        /// Example: "project/database:connectionStrings".
        /// </summary>
        public string GetSecret(string fullKey)
        {
            if (string.IsNullOrEmpty(fullKey))
            {
                _logger.Warning("[VaultService] - Attempted to retrieve a secret with an empty key.");
                throw new ArgumentNullException(nameof(fullKey), "The secret key cannot be null or empty."); 
            }

            if (_secrets.TryGetValue(fullKey, out var value))
            {
                _logger.Debug("[VaultService] - Secret retrieved from cache.");
                return value; 
            }

            if (_vaultClient == null)
            {
                _logger.Error("[VaultService] - Vault client is not connected. Ensure 'Connect()' is called before retrieving secrets.");
                throw new InvalidOperationException("Vault not connected. Call 'Connect()' before retrieving secrets.");
            }

            var parts = fullKey.Split(':');
            if (parts.Length != 2)
            {
                _logger.Error("[VaultService] - Invalid secret key format. Expected format: 'path:key'");
                throw new ArgumentException($"Invalid secret key format. Expected format: 'path:key. Example: 'project/database:connectionStrings'");
            }

            var path = parts[0]; // Example: "project/database"
            var key = parts[1];  // Example: "connectionStrings"

            _logger.Information($"[VaultService] - Fetching secret from '{path}'");
            value = FetchSecretFromVault(path, key);

            if (!string.IsNullOrEmpty(value))
            {
                _secrets.TryAdd(fullKey, value);
                _logger.Debug($"[VaultService] - Secret from path {path} successfully cached.");
                return value;
            }

            _logger.Error($"[VaultService] - Key from path '{path}' not found in Vault.");
            throw new VaultClientException($"Key from path '{path}' not found in Vault.");
        }

        /// <summary>
        /// Retrieves a secret by providing the path and key separately.
        /// </summary>
        public string GetSecret(string path, string key)
        {
            return GetSecret($"{path}:{key}");
        }

        /// <summary>
        /// Fetches a secret directly from Vault.
        /// </summary>
        private string FetchSecretFromVault(string path, string key)
        {
            try
            {
                var fullPath = string.IsNullOrEmpty(_basePath) ? path : $"{_basePath}/{path}";
                _logger.Debug($"[VaultService] - Fetching secret from path '{path}'");

                var secretData = _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(fullPath, mountPoint: _mountPoint).Result;

                if (secretData.Data.Data.TryGetValue(key, out var value))
                {
                    _logger.Information($"[VaultService] - Successfully retrieved secret from path '{path}'");
                    return value?.ToString();
                }

                _logger.Warning($"[VaultService] - Secret not found in path '{path}'.");
                throw new KeyNotFoundException($"Secret not found in path '{path}'.");
            }
            catch (KeyNotFoundException)
            {
                _logger.Warning($"[VaultService] - Secret not found in '{path}'.");
                throw;
            }
            catch (VaultSharp.Core.VaultApiException ex)
            {
                _logger.Error(ex, $"[VaultService] - Vault API error while retrieving secret from '{path}'. Error: {ex}");
                throw new VaultClientException($"Failed to retrieve secret from path '{path}'.", ex);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[VaultService] - Unexpected error while fetching secret from '{path}'. Error: {ex}");
                throw new VaultClientException($"Unexpected error while fetching secret from '{path}'.", ex);
            }
        }

        /// <summary>
        /// Check Vault status
        /// </summary>
        public async Task<bool> CheckVaultHealthAsync()
        {
            try
            {
                _logger.Debug("[VaultService] - Checking Vault health...");

                var healthStatus = await _vaultClient.V1.System.GetHealthStatusAsync();
                var isHealthy = !healthStatus.Sealed;

                _logger.Information($"[VaultService] - Vault health check: {(isHealthy ? "Healthy" : "Sealed/Unavailable")}");

                return !healthStatus.Sealed;
            }
            catch (Exception ex)
            {
                _logger.Error($"[VaultService] - Error checking Vault health: {ex.Message}");
                return false; 
            }
        }

    }
}
