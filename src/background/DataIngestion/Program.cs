using Microsoft.CopilotDashboard.DataIngestion.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;

var host = new HostBuilder()
	.ConfigureFunctionsWebApplication()
	.ConfigureServices((ctx, services) =>
	{
		services.Configure<GithubMetricsApiOptions>(ctx.Configuration.GetSection("GITHUB_METRICS"));
		services.AddHttpClient<GitHubCopilotMetricsClient>(async (sp, hc) => await ConfigureClient(sp, hc));
		services.AddHttpClient<GitHubCopilotUsageClient>(async (sp, hc) => await ConfigureClient(sp, hc));
		services.AddHttpClient<GitHubCopilotApiService>(async (sp, hc) => await ConfigureClient(sp, hc));
		services.AddSingleton<IGitHubTokenService, GitHubTokenService>();
	})
	.Build();

host.Run();

static async Task ConfigureClient(IServiceProvider serviceProvider, HttpClient httpClient)
{
	var gitHubTokenService = serviceProvider.GetRequiredService<IGitHubTokenService>();
	var apiVersion = Environment.GetEnvironmentVariable("GITHUB_API_VERSION");
	//var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
	var token = await gitHubTokenService.FetchTokenFromPem();
	var gitHubApiBaseUrl = Environment.GetEnvironmentVariable("GITHUB_API_BASEURL") ?? "https://api.github.com/";

	httpClient.BaseAddress = new Uri(gitHubApiBaseUrl);
	httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
	httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
	httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", apiVersion);
	httpClient.DefaultRequestHeaders.Add("User-Agent", "GitHubCopilotDataIngestion");
}
