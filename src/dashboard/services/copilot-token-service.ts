import * as jwt from 'jsonwebtoken';

interface IGitHubTokenService {
  fetchTokenFromPem(): Promise<string>;
}

export class GitHubTokenService implements IGitHubTokenService {
  private httpClient: any;

  constructor(httpClient: any) {
    this.httpClient = httpClient;
  }

  async fetchTokenFromPem(): Promise<string> {
    const installationId = process.env.GITHUB_INSTALLATION_ID!;
    const apiToken = await this.fetchGitHubApiToken(installationId);
    return apiToken;
  }

  private fetchGitHubAppToken(): string {
    const privateKey = process.env.GITHUB_PEM!;
    const iat = Math.floor(Date.now() / 1000) - (3 * 60);
    const exp = Math.floor(Date.now() / 1000) + (3 * 60);

    const payload = {
      iat: iat,
      exp: exp,
      iss: process.env.GITHUB_CLIENT_ID!
    };

    const token = jwt.sign(payload, privateKey, { algorithm: 'RS256' });
    return token;
  }

  private async fetchGitHubApiToken(installationId: string): Promise<string> {
    const requestUri = `https://api.github.com/app/installations/${installationId}/access_tokens`;

    const response = await fetch(requestUri, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.fetchGitHubAppToken()}`,
        'Accept': 'application/vnd.github.v3+json'
      },
      body: JSON.stringify({})
    });

    const data = await response.json();
    return data.token;
  }
}
