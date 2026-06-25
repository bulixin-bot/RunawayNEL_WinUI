using System.Text.Json;
using System.Threading.Tasks;
using Codexus.Cipher.Entities.InterConn;
using Codexus.Cipher.Utils.Cipher;
using Codexus.Cipher.Utils.Http;
using Serilog;

namespace Codexus.Cipher.Protocol;

public static class InterConn
{
	private static readonly HttpWrapper Core = new HttpWrapper("https://x19obtcore.nie.netease.com:8443", delegate(HttpWrapper.HttpWrapperBuilder builder)
	{
		builder.UserAgent(WPFLauncher.GetUserAgent());
	});

	public static async Task LoginStart(string entityId, string entityToken)
	{
		Log.Debug<string>("LoginStart response: {0}", await (await Core.PostAsync("/interconn/web/game-play-v2/login-start", "{\"strict_mode\":true}", delegate(HttpWrapper.HttpWrapperBuilder builder)
		{
			builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, entityId, entityToken));
		})).Content.ReadAsStringAsync());
	}

	public static async Task GameStart(string entityId, string entityToken, string gameId)
	{
		Log.Debug<string>("GameStart response: {0}", await (await Core.PostAsync("/interconn/web/game-play-v2/start", JsonSerializer.Serialize(new InterConnGameStart
		{
			GameId = gameId,
			ItemList = new string[1] { "10000" }
		}), delegate(HttpWrapper.HttpWrapperBuilder builder)
		{
			builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, entityId, entityToken));
		})).Content.ReadAsStringAsync());
	}
}
