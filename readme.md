# Azure Secret Rotator - C<span>#</span>

Entra Id app registrations have client secrets that need to be rotated. I needed to automate this process, this azure function app runs every set interval and checks for secrets that are about to expire. It will create a new secret, update the app registration with the new secret, and delete the old secret.
It needs Microsoft Graph API permissions to be able to do this, granted as Application Permission not delegated. The permissions needed are:
- Application.ReadWrite.All

To connect to Microsoft Graph API, the function needs to authenticate. I am using the client credentials flow, which requires the following information:
- Tenant Id
- Client Id
- Client Secret

Store Client secret in Azure Key Vault.

## Requirements
To run locally you need to have a local config file called `local.settings.json` in the root of the project. This file should contain the following keys:
```json
{
  "AzureWebJobsStorage": "useDevelopmentStorage=true",
  "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
  "KeyVaultURI": "<your key vault uri>",
  "ClientSecretName": "<your client secret name in key vault>",
  "TenantId": "<your tenant id>",
  "ClientId": "<your client id>",
  "ClientSecret": "<your client secret>",
  "AppRegistrationObjectId": "<your app registration object id>"
}
```

Also you need [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite).