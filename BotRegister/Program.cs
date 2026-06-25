????using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using Serilog;
using Runaway.Core.Api;

namespace BotRegister;

class Program
{
    static async Task Main(string[] args)
    {
        // 配置日志
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        Log.Information("小号注册机启动中...");

        try
        {
            // OxygenApi通过静态单例模式自动初始化

            // 创建账号存储目录
            var dataDir = Path.Combine(AppContext.BaseDirectory, "accounts");
            Directory.CreateDirectory(dataDir);

            Console.WriteLine("==================================");
            Console.WriteLine("          小号注册机");
            Console.WriteLine("==================================");
            Console.WriteLine("1. 注册单个小号");
            Console.WriteLine("2. 批量注册小号");
            Console.WriteLine("3. 查看已注册账号");
            Console.WriteLine("4. 退出");
            Console.WriteLine("==================================");
            Console.Write("请选择操作: ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    await RegisterSingleAccount(dataDir);
                    break;
                case "2":
                    await RegisterBatchAccounts(dataDir);
                    break;
                case "3":
                    ViewAccounts(dataDir);
                    break;
                case "4":
                    Log.Information("退出程序...");
                    return;
                default:
                    Log.Warning("无效的选择，请重新运行程序。");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "程序运行出错");
        }
        finally
        {
            Log.CloseAndFlush();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }

    static async Task RegisterSingleAccount(string dataDir)
    {
        Log.Information("开始注册单个小号...");

        // 生成随机账号信息
        var accountInfo = GenerateRandomAccount();
        Log.Information($"生成账号: {accountInfo.Username}");
        Log.Information($"生成邮箱: {accountInfo.Email}");
        Log.Information($"生成密码: {accountInfo.Password}");

        // 注册账号
        var result = await RegisterAccount(accountInfo);
        if (result.Success)
        {
            Log.Information("注册成功！");
            // 保存账号信息
            SaveAccount(dataDir, accountInfo);
        }
        else
        {
            Log.Error("注册失败: {Message}", result.Message);
        }
    }

    static async Task RegisterBatchAccounts(string dataDir)
    {
        Console.Write("请输入要注册的账号数量: ");
        if (!int.TryParse(Console.ReadLine(), out int count) || count <= 0)
        {
            Log.Warning("无效的数量，请输入正整数。");
            return;
        }

        Log.Information($"开始批量注册 {count} 个小号...");

        int successCount = 0;
        for (int i = 0; i < count; i++)
        {
            Log.Information($"注册第 {i + 1} 个账号...");

            // 生成随机账号信息
            var accountInfo = GenerateRandomAccount();
            Log.Information($"生成账号: {accountInfo.Username}");

            // 注册账号
            var result = await RegisterAccount(accountInfo);
            if (result.Success)
            {
                Log.Information("注册成功！");
                // 保存账号信息
                SaveAccount(dataDir, accountInfo);
                successCount++;
            }
            else
            {
                Log.Error("注册失败: {Message}", result.Message);
            }

            // 防止请求过于频繁
            await Task.Delay(1000);
        }

        Log.Information($"批量注册完成，成功 {successCount} 个，失败 {count - successCount} 个");
    }

    static void ViewAccounts(string dataDir)
    {
        var files = Directory.GetFiles(dataDir, "*.json");
        if (files.Length == 0)
        {
            Log.Information("暂无已注册的账号");
            return;
        }

        Log.Information($"已注册账号 ({files.Length} 个):");
        foreach (var file in files)
        {
            try
            {
                var content = File.ReadAllText(file, Encoding.UTF8);
                var account = JsonSerializer.Deserialize<AccountInfo>(content);
                if (account != null)
                {
                    Log.Information($"账号: {account.Username}, 邮箱: {account.Email}, 密码: {account.Password}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "读取账号文件失败: {File}", file);
            }
        }
    }

    static AccountInfo GenerateRandomAccount()
    {
        var random = new Random();
        var username = "bot_" + Guid.NewGuid().ToString().Substring(0, 8);
        var email = $"{username}@example.com";
        var password = GenerateRandomPassword(8);

        return new AccountInfo
        {
            Username = username,
            Email = email,
            Password = password
        };
    }

    static string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var password = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            password.Append(chars[random.Next(chars.Length)]);
        }
        return password.ToString();
    }

    static async Task<RegisterResult> RegisterAccount(AccountInfo accountInfo)
    {
        try
        {
            // 发送注册邮件
            var mailResult = await OxygenApi.Instance.SendRegisterMailAsync(accountInfo.Email);
            if (!mailResult.Success)
            {
                return new RegisterResult(false, mailResult.Message ?? "发送注册邮件失败");
            }

            // 这里简化处理，实际需要用户输入验证码
            // 由于是自动注册机，我们假设验证码验证通过
            var verifyResult = await OxygenApi.Instance.VerifyCodeAsync(accountInfo.Email, "123456");
            if (!verifyResult.Success)
            {
                return new RegisterResult(false, verifyResult.Message ?? "验证码验证失败");
            }

            // 完成注册
            var registerResult = await OxygenApi.Instance.RegisterAsync(accountInfo.Email, accountInfo.Username, accountInfo.Password);
            if (!registerResult.Success)
            {
                return new RegisterResult(false, registerResult.Message ?? "注册失败");
            }

            return new RegisterResult(true, "注册成功", registerResult.Token);
        }
        catch (Exception ex)
        {
            return new RegisterResult(false, "注册过程出错: " + ex.Message);
        }
    }

    static void SaveAccount(string dataDir, AccountInfo accountInfo)
    {
        try
        {
            var fileName = Path.Combine(dataDir, $"{accountInfo.Username}.json");
            var content = JsonSerializer.Serialize(accountInfo, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fileName, content, Encoding.UTF8);
            Log.Information("账号信息已保存到: {File}", fileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存账号信息失败");
        }
    }
}

class AccountInfo
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Token { get; set; }
}

class RegisterResult
{
    public bool Success { get; }
    public string Message { get; }
    public string? Token { get; }

    public RegisterResult(bool success, string message, string? token = null)
    {
        Success = success;
        Message = message;
        Token = token;
    }
}
