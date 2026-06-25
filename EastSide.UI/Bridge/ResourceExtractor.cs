using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;

namespace Runaway.UI.Bridge;

public static class ResourceExtractor
{
    private const string Prefix = "Runaway.UI.wwwroot.";

    private static readonly HashSet<string> KnownDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "css", "js", "assets"
    };
    
    private static string GetSafeBaseDir()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return Path.Combine(appData, "Runaway");
    }

    public static string Extract()
    {
        var resourceDir = Path.Combine(GetSafeBaseDir(), "wwwroot");

        var asm = Assembly.GetExecutingAssembly();
        var names = asm.GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal))
            .ToList();

        if (names.Count == 0)
        {
            Log.Warning("未找到嵌入的 wwwroot 资源");
            return resourceDir;
        }

        var extracted = 0;
        foreach (var name in names)
        {
            var remainder = name[Prefix.Length..];
            var relativePath = ResolveRelativePath(remainder);
            var destPath = Path.Combine(resourceDir, relativePath);

            using var stream = asm.GetManifestResourceStream(name);
            if (stream == null) continue;

            var newBytes = new byte[stream.Length];
            stream.ReadExactly(newBytes);

            if (File.Exists(destPath))
            {
                var existing = File.ReadAllBytes(destPath);
                if (existing.AsSpan().SequenceEqual(newBytes)) continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.WriteAllBytes(destPath, newBytes);
            extracted++;
        }

        // 复制根目录的图片文件
        var appBaseDir = AppContext.BaseDirectory;
        Log.Information("应用程序基础目录: {AppBaseDir}", appBaseDir);
        
        // 尝试从应用程序基础目录向上导航到项目根目录
        var currentDir = appBaseDir;
        string? rootDir = null;
        
        // 最多向上导航 5 级目录
        for (int i = 0; i < 5; i++)
        {
            currentDir = Path.GetDirectoryName(currentDir);
            if (currentDir == null)
                break;
            
            // 检查当前目录是否包含 1.jpg 和 2.jpg 文件
            if (currentDir != null && File.Exists(Path.Combine(currentDir, "1.jpg")) && File.Exists(Path.Combine(currentDir, "2.jpg")))
            {
                rootDir = currentDir;
                break;
            }
        }
        
        Log.Information("计算的项目根目录: {RootDir}", rootDir);
        
        var imageFiles = new[] { "1.jpg", "2.jpg" };
        foreach (var imageFile in imageFiles)
        {
            if (rootDir == null)
            {
                Log.Warning("无法找到项目根目录，跳过复制图片文件: {File}", imageFile);
                continue;
            }
            
            var srcPath = Path.Combine(rootDir, imageFile);
            var destPath = Path.Combine(resourceDir, imageFile);
            
            Log.Information("尝试复制图片: 源路�?{Src}, 目标路径={Dest}", srcPath, destPath);
            
            if (File.Exists(srcPath))
            {
                try
                {
                    if (File.Exists(destPath))
                    {
                        var existing = File.ReadAllBytes(destPath);
                        var newBytes = File.ReadAllBytes(srcPath);
                        if (existing.AsSpan().SequenceEqual(newBytes))
                        {
                            Log.Information("图片文件已存在且相同: {File}", imageFile);
                            continue;
                        }
                    }
                    
                    Directory.CreateDirectory(resourceDir);
                    File.Copy(srcPath, destPath, true);
                    extracted++;
                    Log.Information("已成功复制图片文�? {File}", imageFile);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "复制图片文件失败: {File}", imageFile);
                }
            }
            else
            {
                Log.Warning("图片文件不存�? {Path}", srcPath);
            }
        }

        Log.Information("wwwroot 资源已释放到: {Path} (�?{Total} �? 本次释放 {New} �?",
            resourceDir, names.Count + imageFiles.Length, extracted);
        return resourceDir;
    }

    private static string ResolveRelativePath(string remainder)
    {
        var firstDot = remainder.IndexOf('.');
        if (firstDot < 0) return remainder;

        var firstPart = remainder[..firstDot];

        if (KnownDirs.Contains(firstPart))
        {
            var fileName = remainder[(firstDot + 1)..];
            return Path.Combine(firstPart, fileName);
        }

        return remainder;
    }
}

