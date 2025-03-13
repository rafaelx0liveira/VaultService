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
                throw new ArgumentNullException(nameof(vaultAddress), "Vault address cannot be null");

            if (string.IsNullOrWhiteSpace(vaultToken))
                throw new ArgumentNullException(nameof(vaultToken), "Vault token cannot be null");

            var authMethod = new TokenAuthMethodInfo(vaultToken);
            var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod);
            _vaultClient = new VaultSharp.VaultClient(vaultClientSettings);

            _mountPoint = mountPoint;
            _basePath = basePath;

            try
            {
                var healthStatus = _vaultClient.V1.System.GetHealthStatusAsync().Result;
                if (!healthStatus.Sealed)
                {
                    _logger.Information($"[VaultService] - Connected to Vault at {vaultAddress} with mount point '{_mountPoint}'");
                }
                else
                {
                    throw new VaultClientException($"Vault at {vaultAddress} is sealed and cannot be accessed.");
                }
            }
            catch (Exception ex)
            {
                throw new VaultClientException($"Failed to connect to Vault at {vaultAddress}.", ex);
            }
        }

        /// <summary>
        /// Retrieves a secret from Vault in the format "path:key"
        /// Example: "project/database:connectionStrings".
        /// </summary>
        public string GetSecret(string fullKey)
        {
            if (string.IsNullOrEmpty(fullKey))
                throw new ArgumentNullException(nameof(fullKey), "The secret key cannot be null or empty.");

            if (_secrets.TryGetValue(fullKey, out var value))
                return value;

            if (_vaultClient == null)
                throw new InvalidOperationException("Vault not connected. Call 'Connect()' before retrieving secrets.");

            var parts = fullKey.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid format for key '{fullKey}'. Use 'path:key'. Example: 'project/database:connectionStrings'");

            var path = parts[0]; // Example: "project/database"
            var key = parts[1];  // Example: "connectionStrings"

            value = FetchSecretFromVault(path, key);

            if (!string.IsNullOrEmpty(value))
            {
                _secrets.TryAdd(fullKey, value);
                return value;
            }

            throw new VaultClientException($"Key '{fullKey}' not found in Vault.");
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
                var secretData = _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(fullPath, mountPoint: _mountPoint).Result;

                if (secretData.Data.Data.TryGetValue(key, out var value))
                {
                    return value?.ToString();
                }

                throw new KeyNotFoundException($"Secret '{key}' not found in path '{path}'.");
            }
            catch (KeyNotFoundException)
            {
                _logger.Warning($"[VaultService] - Secret '{key}' not found in '{path}'.");
                throw;
            }
            catch (VaultSharp.Core.VaultApiException ex)
            {
                throw new VaultClientException($"Failed to retrieve secret '{key}' from path '{path}'.", ex);
            }
            catch (Exception ex)
            {
                throw new VaultClientException($"Unexpected error while fetching secret '{key}' from '{path}'.", ex);
            }
        }

        /// <summary>
        /// Check Vault status
        /// </summary>
        public async Task<bool> CheckVaultHealthAsync()
        {
            try
            {
                var healthStatus = await _vaultClient.V1.System.GetHealthStatusAsync();
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
