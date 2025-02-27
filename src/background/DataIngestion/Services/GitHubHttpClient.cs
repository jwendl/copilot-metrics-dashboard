using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace Microsoft.CopilotDashboard.DataIngestion.Services;

public interface IGitHubHttpClient
{
	Task<HttpClient> ConfigureHttpClientAsync();
}

public class GitHubHttpClient(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory)
	: IGitHubHttpClient
{
	public async Task<HttpClient> ConfigureHttpClientAsync()
	{
		var httpClient = httpClientFactory.CreateClient();
		var gitHubTokenService = serviceProvider.GetRequiredService<IGitHubTokenService>();
		var apiVersion = Environment.GetEnvironmentVariable("GITHUB_API_VERSION");
		var token = await gitHubTokenService.FetchTokenFromPem();
		var gitHubApiBaseUrl = Environment.GetEnvironmentVariable("GITHUB_API_BASEURL") ?? "https://api.github.com/";

		if (httpClient.BaseAddress == null)
		{
			httpClient.BaseAddress = new Uri(gitHubApiBaseUrl);
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", apiVersion);
			httpClient.DefaultRequestHeaders.Add("User-Agent", "GitHubCopilotDataIngestion");
		}

		return httpClient;
	}
}
