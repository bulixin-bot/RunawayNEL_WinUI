using System.Text.Json.Serialization;

namespace Codexus.Cipher.Entities.WPFLauncher.NetGame;

public class EntityBuyItemResult
{
	[JsonPropertyName("orderid")]
	public string OrderId { get; set; } = string.Empty;

	[JsonPropertyName("buy_type")]
	public int BuyType { get; set; }
}
