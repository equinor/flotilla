using Azure.Security.KeyVault.Secrets;

namespace Api.Services
{

    public class KeyVaultService
    {
        private readonly ILogger<KeyVaultService> _logger;
        private readonly SecretClient _secretClient;

        public KeyVaultService(ILogger<KeyVaultService> logger, SecretClient secretClient)
        {
            _logger = logger;
            _secretClient = secretClient;
        }

        public async Task<string> GetSecret(string secretName)
        {
            try
            {
                KeyVaultSecret keyVaultSecret = await _secretClient.GetSecretAsync(secretName);
                return keyVaultSecret.Value;
            }
            catch
            {
                _logger.LogError("Failed to retrieve secret: {secretName} from keyvault", secretName);
                throw;
            }
        }
    }
}
