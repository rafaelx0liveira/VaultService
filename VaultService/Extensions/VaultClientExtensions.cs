using Microsoft.Extensions.DependencyInjection;
using VaultService.Core;
using VaultService.Interface;

namespace VaultService.Extensions
{
    public static class VaultClientExtensions
    {
        ///<summary>
        /// Add the Vault Service as a service in the dependency injection container
        /// </summary>
        public static IServiceCollection AddVaultService(
                this IServiceCollection services,
                string vaultAddress, 
                string vaultToken,
                string mountPoint,
                string basePath) 
        {
            services.AddSingleton<IVaultClient>(provider =>
            {
                var vaultService = new VaultClient();
                vaultService.Connect(vaultAddress, vaultToken, mountPoint, basePath);
                return vaultService;
            });

            return services;
        }

    }
}
