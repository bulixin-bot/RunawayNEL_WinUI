using System.Text.Json.Serialization;
using Runaway.Enums;

namespace Runaway.Entities.Web.NEL;

public class EntityUpdateUserAlias
{
	[JsonPropertyName("id")]
	public required string EntityId { get; set; }

	[JsonPropertyName("platform")]
	public required Platform Platform { get; set; }

	[JsonPropertyName("alias")]
	public required string Alias { get; set; }
}

