using Microsoft.CopilotDashboard.DataIngestion.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Microsoft.CopilotDashboard.DataIngestion.Services;

public class GitHubCopilotApiService(HttpClient httpClient, IGitHubTokenService gitHubTokenService, ILogger<GitHubCopilotApiService> logger)
{
	public async Task<CopilotAssignedSeats> GetEnterpriseAssignedSeatsAsync(string enterprise)
	{
		//var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")!;
		var token = await gitHubTokenService.FetchTokenFromPem();
		return await GetEnterpriseAssignedSeatsAsync(enterprise, token);
	}

	public async Task<CopilotAssignedSeats> GetEnterpriseAssignedSeatsAsync(string enterprise, string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			logger.LogError("Token is null or empty");
			throw new ArgumentNullException(nameof(token));
		}

		if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
		{
			httpClient.DefaultRequestHeaders.Remove("Authorization");
		}
		httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

		var url = $"/enterprises/{enterprise}/copilot/billing/seats";
		var allSeats = new List<Seat>();
		while (url != null)
		{
			var response = await httpClient.GetAsync(url);
			if (!response.IsSuccessStatusCode)
			{
				logger.LogError($"Error fetching data: {response.StatusCode}", response.Content);
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
		if (string.IsNullOrEmpty(token))
		{
			logger.LogError("Token is null or empty");
			throw new ArgumentNullException(nameof(token));
		}

		if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
		{
			httpClient.DefaultRequestHeaders.Remove("Authorization");
		}
		httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

		var url = $"/orgs/{organization}/copilot/billing/seats";
		var allSeats = new List<Seat>();
		while (!string.IsNullOrEmpty(url))
		{
			var response = await httpClient.GetAsync(url);
			if (!response.IsSuccessStatusCode)
			{
				logger.LogError($"Error fetching data: {response.StatusCode}", response.Content);
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
