using System.Text.Json.Serialization;

namespace Codexus.Cipher.Entities.WPFLauncher.NetGame.Skin;

public class EntityPurchaseResult
{
	[JsonPropertyName("buy_type")]
	public int BuyType { get; set; }

	[JsonPropertyName("entity_id")]
	public string EntityId { get; set; } = string.Empty;
}
