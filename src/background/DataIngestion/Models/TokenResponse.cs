using System.Text.Json.Serialization;

namespace Microsoft.CopilotDashboard.DataIngestion.Models;

public class TokenResponse
{
	[JsonPropertyName("token")]
	public string Token { get; set; } = default!;

	[JsonPropertyName("expires_at")]
	public DateTime ExpiresAt { get; set; }

	[JsonPropertyName("permissions")]
	public Permissions Permissions { get; set; } = default!;

	[JsonPropertyName("repository_selection")]
	public string RepositorySelection { get; set; } = default!;
}

public class Permissions
{
	[JsonPropertyName("organization_copilot_seat_management")]
	public string OrganiztionCopilotSeatManagement { get; set; } = default!;
}
