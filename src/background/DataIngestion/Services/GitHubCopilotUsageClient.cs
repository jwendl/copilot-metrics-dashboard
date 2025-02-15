using System.Text.Json;
using Microsoft.CopilotDashboard.DataIngestion.Models;

namespace Microsoft.CopilotDashboard.DataIngestion.Services;

public class GitHubCopilotUsageClient(IGitHubHttpClient gitHubHttpClient)
{
    public async Task<List<CopilotUsage>> GetCopilotMetricsForOrgsAsync()
    {
        var organization = Environment.GetEnvironmentVariable("GITHUB_ORGANIZATION");
        var httpClient = await gitHubHttpClient.ConfigureHttpClientAsync();
        var response = await httpClient.GetAsync($"/orgs/{organization}/copilot/usage");
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error fetching data: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<List<CopilotUsage>>(content)!;
        return data;
    }

    public async Task<List<CopilotUsage>> GetCopilotMetricsForEnterpriseAsync()
    {
        var enterprise = Environment.GetEnvironmentVariable("GITHUB_ENTERPRISE");
        var httpClient = await gitHubHttpClient.ConfigureHttpClientAsync();
        var response = await httpClient.GetAsync($"/enterprises/{enterprise}/copilot/usage");
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error fetching data: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<List<CopilotUsage>>(content)!;
        return data;
    }
}
