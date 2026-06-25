using System.Text.Json.Serialization;

namespace Codexus.Cipher.Entities.WPFLauncher.NetGame.Skin;

public class EntitySkinItem
{
	[JsonPropertyName("item_id")]
	public string ItemId { get; set; } = string.Empty;
}
