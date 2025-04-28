using Microsoft.CopilotDashboard.DataIngestion.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.CopilotDashboard.DataIngestion.Services;

public interface IGitHubTokenService
{
	Task<string> FetchTokenFromPem();
}

public class GitHubTokenService()
	: IGitHubTokenService
{
	public async Task<string> FetchTokenFromPem()
	{
		var installationId = Environment.GetEnvironmentVariable("GITHUB_INSTALLATION_ID")!;
		var apiToken = await FetchGitHubApiToken(installationId);
		return apiToken.Token;
	}

	private static string FetchGitHubAppToken()
	{
		var pemFile = Environment.GetEnvironmentVariable("GITHUB_PEM")!;
		var pemFileDecodedBytes = Convert.FromBase64String(pemFile);
		var pemFileDecoded = Encoding.UTF8.GetString(pemFileDecodedBytes);
		var rsa = RSA.Create();
		rsa.ImportFromPem(pemFileDecoded.ToCharArray());

		var securityKey = new RsaSecurityKey(rsa);
		var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

		var header = new JwtHeader(signingCredentials);
		var payload = new JwtPayload
		{
			{ "iat", DateTimeOffset.UtcNow.AddMinutes(-3).ToUnixTimeSeconds() },
			{ "exp", DateTimeOffset.UtcNow.AddMinutes(3).ToUnixTimeSeconds() },
			{ "iss", Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID")! }
		};

		var token = new JwtSecurityToken(header, payload);
		var jwtHandler = new JwtSecurityTokenHandler();
		var jwt = jwtHandler.WriteToken(token);
		return jwt;
	}

	private static async Task<TokenResponse> FetchGitHubApiToken(string installationId)
	{
		var gitHubAppToken = FetchGitHubAppToken();
		var apiVersion = Environment.GetEnvironmentVariable("GITHUB_API_VERSION");
		var gitHubApiBaseUrl = Environment.GetEnvironmentVariable("GITHUB_API_BASEURL") ?? "https://api.github.com/";
		var requestUri = $"/app/installations/{installationId}/access_tokens";

		var internalHttpClient = new HttpClient
		{
			BaseAddress = new Uri(gitHubApiBaseUrl)
		};
		internalHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		internalHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gitHubAppToken);
		internalHttpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", apiVersion);
		internalHttpClient.DefaultRequestHeaders.Add("User-Agent", "GitHubCopilotDataIngestion");

		var response = await internalHttpClient.PostAsync(requestUri, new StringContent(""));
		var content = await response.Content.ReadFromJsonAsync<TokenResponse>();
		return content!;
	}
}
