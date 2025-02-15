using Microsoft.Azure.Functions.Worker;
using Microsoft.CopilotDashboard.DataIngestion.Models;
using Microsoft.CopilotDashboard.DataIngestion.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.CopilotDashboard.DataIngestion.Functions;

public class CopilotSeatsIngestion(GitHubCopilotApiService gitHubCopilotApiService, ILogger<CopilotSeatsIngestion> logger)
{
	[Function("GitHubCopilotSeatsIngestion")]
	[CosmosDBOutput(databaseName: "platform-engineering", containerName: "seats_history", Connection = "AZURE_COSMOSDB_ENDPOINT", CreateIfNotExists = true)]

	public async Task<CopilotAssignedSeats> Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
	{
		logger.LogInformation("GitHubCopilotSeatsIngestion timer trigger function executed at: {dateTimeNow}", DateTime.Now);

		CopilotAssignedSeats seats;
		var scope = Environment.GetEnvironmentVariable("GITHUB_API_SCOPE")!;
		_ = Boolean.TryParse(Environment.GetEnvironmentVariable("ENABLE_SEATS_INGESTION") ?? "true", out var seatsIngestionEnabled);
		if (!seatsIngestionEnabled)
		{
			logger.LogInformation("Seats ingestion is disabled");
			return null!;
		}
		if (!string.IsNullOrWhiteSpace(scope) && scope == "enterprise")
		{
			var enterprise = Environment.GetEnvironmentVariable("GITHUB_ENTERPRISE")!;
			logger.LogInformation("Fetching GitHub Copilot seats for enterprise");
			seats = await gitHubCopilotApiService.GetEnterpriseAssignedSeatsAsync(enterprise);
		}
		else
		{
			var organization = Environment.GetEnvironmentVariable("GITHUB_ORGANIZATION")!;
			logger.LogInformation("Fetching GitHub Copilot seats for organization");
			seats = await gitHubCopilotApiService.GetOrganizationAssignedSeatsAsync(organization);
		}

		if (myTimer.ScheduleStatus is not null)
		{
			logger.LogInformation("Finished ingestion. Next timer schedule at: {myTimerScheduleStatusNext}", myTimer.ScheduleStatus.Next);
		}

		return seats;
	}
}
