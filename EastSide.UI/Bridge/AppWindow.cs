using System;
using System.IO;
using Runaway.Manager;
using Photino.NET;
using Serilog;

namespace Runaway.UI.Bridge;

public static class AppWindow
{
    private static PhotinoWindow? _window;
    public static PhotinoWindow? Instance => _window;

    public static void Run()
    {
        var wwwroot = ResourceExtractor.Extract();
        var indexPath = Path.Combine(wwwroot, "index.html");

        if (!File.Exists(indexPath))
        {
            Log.Error("找不到前端入口文件 {Path}", indexPath);
            return;
        }

        var iconPath = Path.Combine(AppContext.BaseDirectory, "641.ico");

        var settings = SettingManager.Instance.Get();

        // Fuck Chinese usernames
        var webviewDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Runaway", "EBWebView");
        Directory.CreateDirectory(webviewDataDir);

        var windowBuilder = new PhotinoWindow()
            .SetTitle("RunawayNEL_WinUI")
            .SetChromeless(true)
            .SetGrantBrowserPermissions(true)
            .SetTemporaryFilesPath(webviewDataDir)
            .SetSize(1200, 750)
            .SetMinSize(900, 600)
            .SetUseOsDefaultSize(false)
            .SetTransparent(true)
            .Center()
            .RegisterWebMessageReceivedHandler(MessageRouter.HandleMessage)
            .RegisterSizeChangedHandler((_, _) => WindowHandler.OnWindowSizeChanged())
            .RegisterWindowCreatedHandler((_, _) =>
            {
                WindowHandler.ApplyRoundedCorners();
                WindowEffects.Apply(settings.Backdrop);
                Log.Information("窗口已初始化，已应用圆角");
            })
            .Load(indexPath);

        // 在窗口创建前设置图标
        if (File.Exists(iconPath))
        {
            windowBuilder.SetIconFile(iconPath);
            Log.Information("已设置窗口图标 {IconPath}", iconPath);
        }
        else
        {
            Log.Warning("图标文件不存在 {IconPath}", iconPath);
        }

        _window = windowBuilder;

        Log.Information("Photino 窗口已创建，加载: {Path}", indexPath);
        _window.WaitForClose();
    }

    public static void PushEvent(string action, object? data = null)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                action,
                requestId = "",
                success = true,
                data
            });
            _window?.SendWebMessage(json);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "推送事件到前端失败: {Action}", action);
        }
    }

    public static void PushNotification(string message, string level)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                action = "notify",
                requestId = "",
                success = true,
                data = new { message, level }
            });
            _window?.SendWebMessage(json);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "推送通知到前端失败");
        }
    }
}

