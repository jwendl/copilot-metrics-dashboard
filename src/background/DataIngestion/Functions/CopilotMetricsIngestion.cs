using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.CopilotDashboard.DataIngestion.Models;
using Microsoft.CopilotDashboard.DataIngestion.Services;

namespace Microsoft.CopilotDashboard.DataIngestion.Functions;

public class CopilotMetricsIngestion(ILogger<CopilotMetricsIngestion> logger, GitHubCopilotMetricsClient metricsClient, IOptions<GithubMetricsApiOptions> options)
{
	[Function("GitHubCopilotMetricsIngestion")]
    [CosmosDBOutput(databaseName: "platform-engineering", containerName: "metrics_history", Connection = "AZURE_COSMOSDB_ENDPOINT", CreateIfNotExists = true)]
    public async Task<List<Metrics>> Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
    {
        logger.LogInformation("GitHubCopilotMetricsIngestion timer trigger function executed at: {dateTimeNow}", DateTime.Now);

        var metrics = new List<Metrics>();

        metrics.AddRange(await ExtractMetrics());

        var teams = options.Value.Teams;
        if (teams != null && teams.Length != 0)
        {
            foreach (var team in teams)
            {
                metrics.AddRange(await ExtractMetrics(team));
            }
        }
        else
        {
            metrics.AddRange(await ExtractMetrics());
        }

        if (myTimer.ScheduleStatus is not null)
        {
            logger.LogInformation("Finished ingestion. Next timer schedule at: {myTimerScheduleStatusNext}", myTimer.ScheduleStatus.Next);
        }
        logger.LogInformation("Metrics count: {metricsCount}", metrics.Count);
        return metrics;
    }

    private async Task<Metrics[]> ExtractMetrics(string? team = null)
    {
        if (options.Value.UseTestData)
        {
            return await LoadTestData(team);
        }

        var scope = Environment.GetEnvironmentVariable("GITHUB_API_SCOPE");
        if (!string.IsNullOrWhiteSpace(scope) && scope == "enterprise")
        {
            logger.LogInformation("Fetching GitHub Copilot usage metrics for enterprise");
            return await metricsClient.GetCopilotMetricsForEnterpriseAsync(team);
        }

        logger.LogInformation("Fetching GitHub Copilot usage metrics for organization");
        return await metricsClient.GetCopilotMetricsForOrganizationAsync(team);
    }

    private ValueTask<Metrics[]> LoadTestData(string? teamName)
    {
        return metricsClient.GetTestCopilotMetrics(teamName);
    }
}
