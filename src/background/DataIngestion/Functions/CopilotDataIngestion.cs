using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.CopilotDashboard.DataIngestion.Models;
using Microsoft.CopilotDashboard.DataIngestion.Services;

namespace Microsoft.CopilotDashboard.DataIngestion.Functions;

public class CopilotDataIngestion(ILogger<CopilotDataIngestion> logger, GitHubCopilotUsageClient usageClient)
{
    private readonly GitHubCopilotUsageClient usageClient = usageClient;

	[Function("GitHubCopilotDataIngestion")]
    [CosmosDBOutput(databaseName: "platform-engineering", containerName: "history", Connection = "AZURE_COSMOSDB_ENDPOINT", CreateIfNotExists = true)]
    public async Task<List<CopilotUsage>> Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
    {
        logger.LogInformation("GitHubCopilotDataIngestion timer trigger function executed at: {dateTimeNow}", DateTime.Now);

        List<CopilotUsage> usage;

        var scope = Environment.GetEnvironmentVariable("GITHUB_API_SCOPE");
        if (!string.IsNullOrWhiteSpace(scope) && scope == "enterprise")
        {
            logger.LogInformation("Fetching GitHub Copilot usage metrics for enterprise");
            usage = await usageClient.GetCopilotMetricsForEnterpriseAsync();
        }
        else
        {
            logger.LogInformation("Fetching GitHub Copilot usage metrics for organization");
            usage = await usageClient.GetCopilotMetricsForOrgsAsync();
        }

        if (myTimer.ScheduleStatus is not null)
        {
            logger.LogInformation("Finished ingestion. Next timer schedule at: {myTimerScheduleStatusNext}", myTimer.ScheduleStatus.Next);
        }

        return usage;
    }
}