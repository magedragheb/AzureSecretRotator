using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureSecretRotator;

public class Timer(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<Timer>();

    [Function("Timer")]
    public void Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {}", DateTime.UtcNow);
        
    }
}
