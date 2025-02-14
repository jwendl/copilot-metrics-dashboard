using Microsoft.Azure.Functions.Worker;
using Microsoft.CopilotDashboard.DataIngestion.Models;
using Microsoft.CopilotDashboard.DataIngestion.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.CopilotDashboard.DataIngestion.Functions;

public class CopilotSeatsIngestion(GitHubCopilotApiService gitHubCopilotApiService, IGitHubTokenService gitHubTokenService, ILogger<CopilotSeatsIngestion> logger)
{
	[Function("GitHubCopilotSeatsIngestion")]
	[CosmosDBOutput(databaseName: "platform-engineering", containerName: "seats_history", Connection = "AZURE_COSMOSDB_ENDPOINT", CreateIfNotExists = true)]

	public async Task<CopilotAssignedSeats> Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
	{
		logger.LogInformation($"GitHubCopilotSeatsIngestion timer trigger function executed at: {DateTime.Now}");

		CopilotAssignedSeats seats;

		//var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")!;
		var token = await gitHubTokenService.FetchTokenFromPem();
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
			seats = await gitHubCopilotApiService.GetEnterpriseAssignedSeatsAsync(enterprise, token);
		}
		else
		{
			var organization = Environment.GetEnvironmentVariable("GITHUB_ORGANIZATION")!;
			logger.LogInformation("Fetching GitHub Copilot seats for organization");
			seats = await gitHubCopilotApiService.GetOrganizationAssignedSeatsAsync(organization, token);
		}

		if (myTimer.ScheduleStatus is not null)
		{
			logger.LogInformation($"Finished ingestion. Next timer schedule at: {myTimer.ScheduleStatus.Next}");
		}

		return seats;
	}
}
