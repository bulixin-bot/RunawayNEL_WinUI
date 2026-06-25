using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Codexus.Cipher.Entities.Com4399;
using Codexus.Cipher.Extensions.Com4399Extensions;
using Codexus.Cipher.Utils.Http;
using Codexus.Development.SDK.Utils;
using Serilog;

namespace Codexus.Cipher.Protocol;

public partial class Com4399
{
	private const string Com4399File = "4399com.cds";

	private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	private readonly HttpWrapper _4399Api = new HttpWrapper("https://m.4399api.com");

	private readonly Lock _lock = new Lock();

	private readonly HttpWrapper _login = new HttpWrapper("https://ptlogin.4399.com", builder =>
	{
		builder.UserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
		builder.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
		builder.AddHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
		builder.AddHeader("Accept-Encoding", "gzip, deflate, br");
		builder.AddHeader("Connection", "keep-alive");
		builder.AddHeader("Upgrade-Insecure-Requests", "1");
		builder.AddHeader("sec-ch-ua", "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"");
		builder.AddHeader("sec-ch-ua-mobile", "?0");
		builder.AddHeader("sec-ch-ua-platform", "\"Windows\"");
		builder.AddHeader("Sec-Fetch-Dest", "document");
		builder.AddHeader("Sec-Fetch-Mode", "navigate");
		builder.AddHeader("Sec-Fetch-Site", "same-site");
		builder.AddHeader("Sec-Fetch-User", "?1");
		builder.AddHeader("Origin", "https://ptlogin.4399.com");
	}, new HttpClientHandler
	{
		AllowAutoRedirect = false,
		UseCookies = true,
		CookieContainer = new System.Net.CookieContainer(),
		AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
	});

	private readonly MgbSdk _mgbSdk = new MgbSdk("x19");

	private readonly WebNexusApi _nexus = new WebNexusApi("YXBwSWQ9Q29kZXh1cy5HYXRld2F5LmFwcFNlY3JldD1hN0s5bTJYcUw4YkM0d1ox");

	private string _deviceIdentifier = string.Empty;

	private string _deviceIdentifierSm = string.Empty;

	private string _state = string.Empty;

	private string _udid = string.Empty;

	public Com4399()
	{
		_login.GetClient().DefaultRequestHeaders.Referrer = new Uri("https://ptlogin.4399.com/");
		CreateOrLoadDevice().GetAwaiter().GetResult();
	}

	private async Task CreateOrLoadDevice()
	{
		byte[] array = MPay.LoadFromFile("4399com.cds");
		Entity4399Device entity4399Device = ((array != null) ? LoadDevice(array) : (await CreateDevice()));
		Entity4399Device entity4399Device2 = entity4399Device;
		if (entity4399Device2.DeviceState == null)
		{
			entity4399Device2 = await CreateDevice();
		}
		_deviceIdentifier = entity4399Device2.DeviceIdentifier;
		_deviceIdentifierSm = entity4399Device2.DeviceIdentifierSm;
		_udid = entity4399Device2.DeviceUdid;
		_state = entity4399Device2.DeviceState;
	}

	private static Entity4399Device LoadDevice(byte[] data)
	{
		return JsonSerializer.Deserialize<Entity4399Device>(data);
	}

	private async Task<Entity4399Device> CreateDevice()
	{
		_deviceIdentifier = GenerateIdentifier();
		_deviceIdentifierSm = GenerateIdentifier();
		_udid = Guid.NewGuid().ToString();
		string deviceState = await OAuthDevice();
		Entity4399Device entity4399Device = new Entity4399Device
		{
			DeviceIdentifier = _deviceIdentifier,
			DeviceIdentifierSm = _deviceIdentifierSm,
			DeviceUdid = _udid,
			DeviceState = deviceState
		};
		using (_lock.EnterScope())
		{
			MPay.SaveToFile("4399com.cds", JsonSerializer.Serialize(entity4399Device, DefaultOptions));
			return entity4399Device;
		}
	}

	private async Task<string> OAuthDevice()
	{
		string body = new ParameterBuilder().Append("usernames", "").Append("top_bar", "1").Append("state", "")
			.Append("device", JsonSerializer.Serialize(new Entity4399OAuth
			{
				DeviceIdentifier = _deviceIdentifier,
				DeviceIdentifierSm = _deviceIdentifierSm,
				Udid = _udid
			}, DefaultOptions))
			.FormUrlEncode();
		HttpResponseMessage response = await _4399Api.PostAsync("/openapiv2/oauth.html", body, "application/x-www-form-urlencoded");
		response.EnsureSuccessStatusCode();
		return new ParameterBuilder((JsonSerializer.Deserialize<Entity4399OAuthResponse>(await response.Content.ReadAsStringAsync()) ?? throw new Exception("Failed to deserialize: " + response.Content.ReadAsStringAsync().Result)).Result.LoginUrl).Get("state");
	}

	public async Task<string> LoginAndAuthorize(string username, string password, string? captcha = null, string? captchaId = null, int retry = 0)
	{
		if (retry > 5)
		{
			throw new Exception("Retry limit exceeded");
		}

		// Use OAuthDevice to get state (original approach)
		string state = await OAuthDevice();

		// Build login parameters (original set with OAuthDevice state)
		ParameterBuilder parameterBuilder = new ParameterBuilder()
			.Append("isInputRealname", "false")
			.Append("isValidRealname", "false")
			.Append("sec", "1")
			.Append("client_id", "40f9e9b95d6c71ba5c6e0bd14c0abeff")
			.Append("state", state)
			.Append("ref", "{\"game\":\"115716\",\"channel\":\"\"}")
			.Append("response_type", "TOKEN")
			.Append("scope", "basic")
			.Append("bizId", "2100001792")
			.Append("auth_action", "ORILOGIN")
			.Append("redirect_uri", "https://m.4399api.com/openapi/oauth-callback.html?gamekey=44770&game_key=115716")
			.Append("reg_mode", "reg_phone")
			.Append("show_topbar", "false")
			.Append("password", password)
			.Append("username", username.ToLowerInvariant());
		
		if (captcha != null && captchaId != null)
		{
			parameterBuilder.Append("captcha_id", captcha).Append("captcha", captchaId);
		}

		string body = parameterBuilder.FormUrlEncode();
		Log.Information("Executing loginAndAuthorize...");

		// Use a simple HttpClient like NCom4399 does (auto-redirect enabled)
		using var client = new HttpClient(new HttpClientHandler
		{
			UseCookies = true,
			CookieContainer = new System.Net.CookieContainer()
		});
		HttpResponseMessage loginResponse = await client.PostAsync(
			"https://ptlogin.4399.com/oauth2/loginAndAuthorize.do?channel=&sdk=op&sdk_version=3.14.5.577",
			new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded"));

		string loginText = await loginResponse.Content.ReadAsStringAsync();

		// Extract error from HTML response
		string errText = ExtractErrorTip(loginText);
		if (!string.IsNullOrEmpty(errText))
		{
			throw new Exception(errText);
		}

		Entity4399UserInfoResponse entity4399UserInfoResponse = JsonSerializer.Deserialize<Entity4399UserInfoResponse>(loginText);
		if (entity4399UserInfoResponse == null)
		{
			throw new Exception("Failed to deserialize: " + loginText);
		}

		if (entity4399UserInfoResponse.Code != "100")
		{
			throw new Exception(entity4399UserInfoResponse.Message);
		}

		Entity4399UserInfoResult result = entity4399UserInfoResponse.Result;
		if (result == null)
		{
			throw new Exception("Failed to deserialize: " + loginText);
		}

		return _mgbSdk.GenerateSAuth(Guid.NewGuid().ToString("N").ToUpper(), "", result.Uid.ToString(), result.State, "", "4399com", "ad");
	}

	private static string ExtractErrorTip(string html)
	{
		const string startMarker = "login_err_msg\">";
		const string endMarker = "</p>";

		var startIndex = html.IndexOf(startMarker, StringComparison.Ordinal);
		if (startIndex == -1) return string.Empty;

		startIndex += startMarker.Length;
		var endIndex = html.IndexOf(endMarker, startIndex, StringComparison.Ordinal);

		if (endIndex == -1) return string.Empty;

		var content = html.Substring(startIndex, endIndex - startIndex);
		return content.Trim();
	}

	private async Task<string> HandleCaptchaWithHtml(string username, string password, string html, int retry)
	{
		if (retry >= 5)
		{
			throw new Exception("验证码重试次数超限");
		}
		Match match = CaptchaRegex().Match(html);
		if (!match.Success)
		{
			throw new Exception("Cannot find captcha in html");
		}
		string matchedCaptchaId = match.Groups[1].Value;
		HttpResponseMessage obj = await _login.GetAsync("/ptlogin/captcha.do?captchaId=" + matchedCaptchaId + "&xx=1");
		WebNexusApi nexus = _nexus;
		string captcha = nexus.ComputeCaptchaAsync(await obj.Content.ReadAsByteArrayAsync());
		return await LoginAndAuthorize(username, password, captcha, matchedCaptchaId, retry + 1);
	}

	private static string GenerateIdentifier(DateTime? dateTime = null, string? additionalData = null)
	{
		string text = (dateTime ?? DateTime.Now).ToString("yyyyMMddHHmm");
		string text2 = GenerateHash50(additionalData);
		return text + text2;
	}

	private static string GenerateHash50(string? data = null)
	{
		if (string.IsNullOrEmpty(data))
		{
			data = Guid.NewGuid().ToString() + DateTime.Now.Ticks;
		}
		return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(data))).Substring(0, 50);
	}

	[GeneratedRegex("name\\s*=\\s*[\"\"']captcha_id[\"\"']\\s+value\\s*=\\s*[\"\"']([^\"\"']+)[\"\"']", RegexOptions.IgnoreCase, "zh-CN")]
	private static partial Regex CaptchaRegex();
}
