using Serilog;

namespace VaultService.Exceptions
{
    public class VaultServiceException : Exception
    {
        private static readonly ILogger _logger = Log.ForContext<VaultServiceException>();

        public VaultServiceException(string message) : base(message) 
        {
            _logger.Error($"[VaultServiceException] - {message}");
        }

        public VaultServiceException(string message, Exception innerException) : base(message, innerException)
        {
            _logger.Error(innerException, $"[VaultServiceException] - {message}");
        }
    }
}
