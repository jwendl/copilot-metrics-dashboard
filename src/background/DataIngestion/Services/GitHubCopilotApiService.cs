using Microsoft.CopilotDashboard.DataIngestion.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Microsoft.CopilotDashboard.DataIngestion.Services;

public class GitHubCopilotApiService(IGitHubHttpClient gitHubHttpClient, IGitHubTokenService gitHubTokenService, ILogger<GitHubCopilotApiService> logger)
{
	public async Task<CopilotAssignedSeats> GetEnterpriseAssignedSeatsAsync(string enterprise)
	{
		var httpClient = await gitHubHttpClient.ConfigureHttpClientAsync();
		var url = $"/enterprises/{enterprise}/copilot/billing/seats";
		var allSeats = new List<Seat>();
		while (url != null)
		{
			var response = await httpClient.GetAsync(url);
			if (!response.IsSuccessStatusCode)
			{
				logger.LogError("Error fetching data: {responseStatusCode}{newLine}{responseContent}", response.StatusCode, Environment.NewLine, response.Content);
				throw new HttpRequestException($"Error fetching data: {response.StatusCode}");
			}
			var content = await response.Content.ReadAsStringAsync();
			var data = JsonSerializer.Deserialize<CopilotAssignedSeats>(content)!;
			allSeats.AddRange(data.Seats!);

			url = Helpers.GetNextPageUrl(response.Headers);
		}

		return new CopilotAssignedSeats
		{
			TotalSeats = allSeats.Count,
			Enterprise = enterprise,
			LastUpdate = DateTime.UtcNow,
			Date = DateOnly.FromDateTime(DateTime.UtcNow),
			Seats = allSeats
		};

	}

	public async Task<CopilotAssignedSeats> GetOrganizationAssignedSeatsAsync(string organization)
	{
		//var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")!;
		var token = await gitHubTokenService.FetchTokenFromPem();
		return await GetOrganizationAssignedSeatsAsync(organization, token);
	}

	public async Task<CopilotAssignedSeats> GetOrganizationAssignedSeatsAsync(string organization, string token)
	{
		var httpClient = await gitHubHttpClient.ConfigureHttpClientAsync();

		var url = $"/orgs/{organization}/copilot/billing/seats";
		var allSeats = new List<Seat>();
		while (!string.IsNullOrEmpty(url))
		{
			var response = await httpClient.GetAsync(url);
			if (!response.IsSuccessStatusCode)
			{
				logger.LogError("Error fetching data: {responseStatusCode}{newLine}{responseContent}", response.StatusCode, Environment.NewLine, response.Content);
				throw new HttpRequestException($"Error fetching data: {response.StatusCode}");
			}
			var content = await response.Content.ReadAsStringAsync();
			var data = JsonSerializer.Deserialize<CopilotAssignedSeats>(content)!;
			allSeats.AddRange(data.Seats!);

			url = Helpers.GetNextPageUrl(response.Headers);
		}

		return new CopilotAssignedSeats
		{
			TotalSeats = allSeats.Count,
			Organization = organization,
			LastUpdate = DateTime.UtcNow,
			Date = DateOnly.FromDateTime(DateTime.UtcNow),
			Seats = allSeats
		};
	}
}
