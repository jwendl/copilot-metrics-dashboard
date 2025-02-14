import * as fs from 'fs';
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
    const gitHubAppToken = this.fetchGitHubAppToken();
    const apiToken = await this.fetchGitHubApiToken(installationId);
    return apiToken;
  }

  private fetchGitHubAppToken(): string {
    const pemFile = process.env.GITHUB_PEM!;
    const privateKey = fs.readFileSync(pemFile, 'utf8');

    const payload = {
      iat: Math.floor(Date.now() / 1000) - 60,
      exp: Math.floor(Date.now() / 1000) + (10 * 60),
      iss: process.env.GITHUB_CLIENT_ID!
    };

    const token = jwt.sign(payload, privateKey, { algorithm: 'RS256' });
    return token;
  }

  private async fetchGitHubApiToken(installationId: string): Promise<string> {
    const requestUri = `/app/installations/${installationId}/access_tokens`;

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
