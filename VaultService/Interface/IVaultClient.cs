namespace VaultService.Interface
{
    public interface IVaultClient
    {
        string GetSecret(string fullKey);
        string GetSecret(string path, string key);
        Task<bool> CheckVaultHealthAsync();
    }
}
