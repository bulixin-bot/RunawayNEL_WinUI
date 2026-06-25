using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Codexus.Cipher.Connection.Protocols;
using Codexus.Cipher.Protocol;
using Codexus.Development.SDK.Manager;
using Codexus.Game.Launcher.Utils;
using Codexus.Interceptors;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenSDK.Yggdrasil;
using Runaway.Core.Network;
using Runaway.IRC;
using Runaway.Manager;
using Runaway.Type;
using Runaway.Utils;
using Serilog;
using FileUtil = Codexus.Game.Launcher.Utils.FileUtil;

namespace Runaway;

public static class Backend
{
    private static readonly TaskCompletionSource _initialized = new();
    private const string RandomLoginApiUrl = "https://cookie.meowow.org/api/accounts/credentials/quick";
    private const string RandomLoginApiKey = "你的api";
    private const string CredentialsApiUrl = "https://cookie.meowow.org/api/accounts/credentials/quick";
    private const string CredentialsApiKey = "你的api";
    private static readonly HttpClient RandomLoginHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };
    private static readonly HttpClient CredentialsHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public static Task WaitForInitAsync() => _initialized.Task;

    public static void Initialize()
    {
        AuthManager.Instance.LoadFromDisk();
        LocalHttpServer.Instance.Start();
        IdentifierServer.Instance.ChannelLookup = LookupChannel;
        IdentifierServer.Instance.OnChannelMarked = addr => IrcEventHandler.MarkSouthside(addr);
        IdentifierServer.Instance.Start();
        _ = InitializeServicesAsync();
    }

    private static async Task InitializeServicesAsync()
    {
        try
        {
            await Task.Run(async () =>
            {
                FileUtil.CreateDirectorySafe(PathUtil.ResourcePath);
                AppState.Services = await CreateServicesAsync();
                InternalQuery.Initialize();
                await InitializeSystemComponentsAsync();
                Notification.Send("Runaway","Welcome!");
            });
            _initialized.TrySetResult();
            Log.Information("后端服务初始化完成");
        }
        catch (Exception ex)
        {
            _initialized.TrySetResult();
            Log.Information($"服务初始化失败 {ex.Message}");
        }
    }

    private static async Task<Services> CreateServicesAsync()
    {
        var launcherVersion = await WPFLauncher.GetLatestVersionAsync();
        var yggdrasil = new StandardYggdrasil(new YggdrasilData
        {
            LauncherVersion = launcherVersion,
            Channel = "netease",
            CrcSalt = "E77652A5A6FE19810998B02347F2D805"
        });
        return new Services(yggdrasil);
    }

    private static async Task InitializeSystemComponentsAsync()
    {
        var pluginDir = Runaway.Utils.FileUtil.GetPluginDirectory();
        Directory.CreateDirectory(pluginDir);
        UserManager.Instance.ReadUsersFromDisk();
        Interceptor.EnsureLoaded();
        PacketManager.Instance.RegisterPacketFromAssembly(typeof(Backend).Assembly);
        PacketManager.Instance.RegisterPacketFromAssembly(typeof(IrcManager).Assembly);
        PacketManager.Instance.EnsureRegistered();
        RegisterIrcHandler();
        HttpUrlRewriter.Initialize();
        try
        {
            PluginManager.Instance.EnsureUninstall();
            PluginManager.Instance.LoadPlugins(pluginDir);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "插件加载失败");
        }
        await Task.CompletedTask;
    }
    
    static void RegisterIrcHandler()
    {
        IrcEventHandler.Register(() => AuthManager.Instance.Token);
        IrcManager.IrcHintEnabledProvider = () => SettingManager.Instance.Get().IrcHintEnabled;
        IrcManager.IrcHintIntervalProvider = () => SettingManager.Instance.Get().IrcHintInterval;
        IrcManager.SkinLookupProvider = async (playerName, gameId) =>
        {
            try
            {
                var user = UserManager.Instance.GetLastAvailableUser();
                if (user == null) return null;
                return await NeteaseSkinLookup.LookupAsync(playerName, gameId, user.UserId, user.AccessToken);
            }
            catch { return null; }
        };
        IrcEventHandler.LocalAddressLookup = id =>
        {
            var interceptor = GameManager.Instance.GetInterceptor(id);
            if (interceptor == null) return null;
            return $"{interceptor.LocalAddress}:{interceptor.LocalPort}";
        };
    }

    private static ChannelInfo? LookupChannel(string address)
    {
        var interceptors = GameManager.Instance.GetQueryInterceptors();
        var match = interceptors.FirstOrDefault(i =>
            string.Equals(i.LocalAddress, address, StringComparison.OrdinalIgnoreCase));
        if (match == null) return null;

        return new ChannelInfo
        {
            Identifier = match.Name.ToString(),
            ServerName = match.Server,
            RoleName = match.Role,
            ServerVersion = match.Version,
            LocalAddress = match.LocalAddress,
            ForwardAddress = match.Address
        };
    }
    
    public static async Task<(bool Success, string Message)> RandomLogin4399Async()
    {
        try
        {
            var cookie = await FetchRandomLoginCookieAsync();
            return await LoginCookieAsync(cookie);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "随机登录失败");
            return (false, ex.Message);
        }
    }

    public static async Task<(bool Success, string Message)> RandomLogin4399WithCredentialsAsync()
    {
        try
        {
            // 1. Fetch account/password from credentials API
            using var request = new HttpRequestMessage(HttpMethod.Post, CredentialsApiUrl);
            request.Headers.Add("X-Api-Key", CredentialsApiKey);

            using var response = await CredentialsHttpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"取号失败: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            string account = root.GetProperty("account").GetString() ?? "";
            string password = root.GetProperty("password").GetString() ?? "";

            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
            {
                throw new Exception("取号失败: 返回数据不完整");
            }

            Log.Information("获取到随机账号: {Account}", account);

            // 2. Login via Com4399Login (same flow as Login4399 handler)
            InternalQuery.Initialize();
            string peCookie = await Runaway.Protocol.Com4399Login.LoginWithPasswordAsync(account, password, "", "");
            string pcCookie = await Task.Run(() => new Pc4399().LoginWithPasswordAsync(account, password, "", "").GetAwaiter().GetResult());

            bool useMixed = SettingManager.Instance.Get().UseMixedLogin;
            string loginCookie = useMixed ? peCookie : pcCookie;

            if (string.IsNullOrWhiteSpace(loginCookie))
            {
                return (false, useMixed ? "Failed to get PE cookie" : "Failed to get PC cookie");
            }

            var (authOtp, channel) = AppState.X19.LoginWithCookie(loginCookie);
            Log.Information("随机登录成功: {UserId} Channel: {Channel}", authOtp.EntityId, channel);

            UserManager.Instance.AddUserToMaintain(authOtp);
            UserManager.Instance.AddUser(new Entities.Web.EntityUser
            {
                UserId = authOtp.EntityId,
                Authorized = true,
                AutoLogin = false,
                Channel = channel,
                Type = "password",
                Details = JsonSerializer.Serialize(new Entities.Web.NEL.EntityPasswordRequest
                {
                    Account = account,
                    Password = password
                })
            });

            // Save to disk to persist the account list
            await UserManager.Instance.SaveUsersToDiskAsync();

            return (true, "登录成功");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "随机登录失败");
            return (false, ex.Message);
        }
    }

    private static async Task<string> FetchRandomLoginCookieAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, RandomLoginApiUrl);
        request.Headers.Add("X-Api-Key", RandomLoginApiKey);

        using var response = await RandomLoginHttpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"取号失败: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        using var document = JsonDocument.Parse(content);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new Exception("取号失败: 返回格式无效");
        }

        var sauthJson = document.RootElement.GetRawText();
        var wrappedCookie = JsonSerializer.Serialize(new { sauth_json = sauthJson });
        Log.Information("随机登录取号成功，准备进行 Cookie 一键登录");
        return wrappedCookie;
    }

    private static async Task<(bool Success, string Message, string? Extra)> ParseLoginResultAsync(object? result)
    {
        if (result == null) return (false, "登录失败", null);

        var resultType = result.GetType();
        var typeProp = resultType.GetProperty("type");
        if (typeProp != null)
        {
            var typeValue = typeProp.GetValue(result)?.ToString() ?? "";

            if (typeValue.EndsWith("_error", StringComparison.OrdinalIgnoreCase))
            {
                var msg = resultType.GetProperty("message")?.GetValue(result)?.ToString() ?? "登录失败";
                return (false, msg, null);
            }

            if (typeValue.StartsWith("captcha_required"))
            {
                var captchaUrl = resultType.GetProperty("captchaUrl")?.GetValue(result)?.ToString();
                return (false, "需要验证码", captchaUrl);
            }

            if (typeValue == "login_x19_verify")
            {
                var verifyUrl = resultType.GetProperty("verify_url")?.GetValue(result)?.ToString();
                return (false, "需要安全验证，请在浏览器中完成", verifyUrl);
            }
        }

        if (result is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var tv = item?.GetType().GetProperty("type")?.GetValue(item)?.ToString();
                if (tv == "Success_login")
                {
                    var eid = item?.GetType().GetProperty("entityId")?.GetValue(item)?.ToString();
                    await Manager.UserManager.Instance.SaveUsersToDiskAsync();
                    return (true, $"登录成功！EntityId: {eid}", null);
                }
            }
        }

        return (false, "登录失败", null);
    }

    public static async Task<(bool Success, string Message)> Login4399Async(string account, string password)
    {
        try
        {
            var handler = new Handlers.PC.Login.Login4399();
            var result = await Task.Run(() => handler.Execute(account, password));
            var parsed = await ParseLoginResultAsync(result);
            return (parsed.Success, parsed.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "4399登录失败");
            return (false, ex.Message);
        }
    }

    public static async Task<(bool Success, string Message)> LoginNeteaseAsync(string email, string password)
    {
        try
        {
            var handler = new Handlers.PC.Login.LoginX19();
            var result = await Task.Run(() => handler.Execute(email, password));
            var parsed = await ParseLoginResultAsync(result);
            return (parsed.Success, parsed.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "网易邮箱登录失败");
            return (false, ex.Message);
        }
    }

    public static async Task<(bool Success, string Message)> LoginCookieAsync(string cookie)
    {
        try
        {
            var handler = new Handlers.PC.Login.LoginCookie();
            var result = await Task.Run(() => handler.Execute(cookie));
            var parsed = await ParseLoginResultAsync(result);
            return (parsed.Success, parsed.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cookie登录失败");
            return (false, ex.Message);
        }
    }
}
