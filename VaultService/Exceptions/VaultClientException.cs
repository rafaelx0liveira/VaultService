using Serilog;

namespace VaultService.Exceptions
{
    public class VaultClientException : Exception
    {
        private static readonly ILogger _logger = Log.ForContext<VaultClientException>();

        public VaultClientException(string message) : base(message) 
        {
            _logger.Error($"[VaultServiceException] - {message}");
        }

        public VaultClientException(string message, Exception innerException) : base(message, innerException)
        {
            _logger.Error(innerException, $"[VaultServiceException] - {message}");
        }
    }
}
