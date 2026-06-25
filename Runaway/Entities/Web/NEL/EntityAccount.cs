using System.Text.Json.Serialization;

namespace Runaway.Entities.Web.NEL;

public class EntityAccount
{
	[JsonPropertyName("id")]
	public required string UserId { get; set; }

	[JsonPropertyName("alias")]
	public required string Alias { get; set; }
}

