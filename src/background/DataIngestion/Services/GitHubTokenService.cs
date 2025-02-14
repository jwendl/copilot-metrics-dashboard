using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace Microsoft.CopilotDashboard.DataIngestion.Services;

public interface IGitHubTokenService
{
	Task<string> FetchTokenFromPem();
}

public class GitHubTokenService(HttpClient httpClient)
	: IGitHubTokenService
{
	public async Task<string> FetchTokenFromPem()
	{
		var installationId = Environment.GetEnvironmentVariable("GITHUB_INSTALLATION_ID")!;
		var gitHubAppToken = FetchGitHubAppToken();
		var apiToken = await FetchGitHubApiToken(installationId);
		return apiToken;
	}

	private static string FetchGitHubAppToken()
	{
		var pemFile = Environment.GetEnvironmentVariable("GITHUB_PEM")!;
		var rsa = RSA.Create();
		rsa.ImportFromPem(pemFile.ToCharArray());

		var securityKey = new RsaSecurityKey(rsa);
		var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

		var header = new JwtHeader(signingCredentials);
		var payload = new JwtPayload
		{
			{ "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 60 },
			{ "exp", DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds() - (10 * 60) },
			{ "iss", Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID")! }
		};

		var token = new JwtSecurityToken(header, payload);
		var jwtHandler = new JwtSecurityTokenHandler();
		var jwt = jwtHandler.WriteToken(token);
		return jwt;
	}

	private async Task<string> FetchGitHubApiToken(string installationId)
	{
		var requestUri = $"/app/installations/{installationId}/access_tokens";

		var response = await httpClient.PostAsync(requestUri, new StringContent(string.Empty));
		var content = await response.Content.ReadAsStringAsync();
		return content;
	}
}
