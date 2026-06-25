using System.Text.Json.Serialization;

namespace Codexus.Cipher.Entities.Com4399;

public class Entity4399OAuthCallback
{
	[JsonPropertyName("result")]
	public string Result { get; set; } = string.Empty;
}
