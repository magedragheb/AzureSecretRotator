using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Models;

namespace AzureSecretRotator;

public class Timer(ILoggerFactory loggerFactory, IConfiguration config)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<Timer>();

    [Function("Timer")]
    public async Task Run([TimerTrigger("0 0 */175 * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {}", DateTime.UtcNow.ToLocalTime());

        string TenantId = config["TenantId"]!;
        string ClientId = config["ClientId"]!;
        string secretName = config["ClientSecretName"]!;
        string ClientSecret = await GetKeyVaultSecret(secretName);
        string AppObjectId = config["AppObjectId"]!;
        string[] Scopes = ["https://graph.microsoft.com/.default"];

        if (string.IsNullOrEmpty(TenantId) || string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(secretName) || string.IsNullOrEmpty(ClientSecret) || string.IsNullOrEmpty(AppObjectId))
        {
            _logger.LogError("Missing configuration values. Please check your settings.");
            return;
        }

        var clientSecretCredential = new ClientSecretCredential(TenantId, ClientId, ClientSecret);
        var graphClient = new GraphServiceClient(clientSecretCredential, Scopes);

        await AddAppSecret(graphClient, AppObjectId);
        await RemoveExpiredSecret(graphClient, AppObjectId);

        _logger.LogInformation("Secret rotation completed at: {}", DateTime.UtcNow.ToLocalTime());

        graphClient.Dispose();

    }

    public async Task<string> GetKeyVaultSecret(string secretName)
    {
        try
        {
            var client = new SecretClient(new Uri(config["KeyVaultURI"]!), new DefaultAzureCredential());
            var secret = await client.GetSecretAsync(secretName);
            return secret.Value.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error reading keyvault secret: {}", ex.Message);
            throw;
        }
    }


    public async Task AddAppSecret(GraphServiceClient graphClient, string AppObjectId)
    {
        var requestBody = new AddPasswordPostRequestBody
        {
            PasswordCredential = new PasswordCredential
            {
                DisplayName = config["ClientSecretName"],
                EndDateTime = DateTime.UtcNow.AddDays(180)
            },
        };
        try
        {
            var result = await graphClient.Applications[AppObjectId].AddPassword.PostAsync(requestBody);
            if (result is not null && result.SecretText is not null)
                await UpdateKeyVaultSecret(config["ClientSecretName"]!, result.SecretText);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error adding app secret: {}", ex.Message);
            throw;
        }
    }

    public async Task RemoveExpiredSecret(GraphServiceClient graphClient, string AppObjectId)
    {
        var app = await graphClient.Applications[AppObjectId].GetAsync();
        if (app is not null && app.PasswordCredentials is not null)
        {
            app.PasswordCredentials.RemoveAll(x => x.EndDateTime < DateTime.UtcNow);
            try
            {
                await graphClient.Applications[AppObjectId].PatchAsync(app);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error removing expired secret: {}", ex.Message);
                throw;
            }
        }
    }

    public async Task UpdateKeyVaultSecret(string secretName, string secretValue)
    {
        try
        {
            var client = new SecretClient(new Uri(config["KeyVaultURI"]!), new DefaultAzureCredential());
            var newSecret = new KeyVaultSecret(secretName, secretValue);
            await client.SetSecretAsync(newSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating keyvault secret: {}", ex.Message);
            throw;
        }
    }
}
