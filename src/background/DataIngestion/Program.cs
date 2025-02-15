using Microsoft.CopilotDashboard.DataIngestion.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
	.ConfigureFunctionsWebApplication()
	.ConfigureServices((ctx, services) =>
	{
		services.Configure<GithubMetricsApiOptions>(ctx.Configuration.GetSection("GITHUB_METRICS"));
		services.AddHttpClient<IGitHubHttpClient, GitHubHttpClient>();
		services.AddSingleton<GitHubCopilotMetricsClient>();
		services.AddSingleton<GitHubCopilotUsageClient>();
		services.AddSingleton<GitHubCopilotApiService>();
		services.AddSingleton<IGitHubTokenService, GitHubTokenService>();
	})
	.Build();

host.Run();
