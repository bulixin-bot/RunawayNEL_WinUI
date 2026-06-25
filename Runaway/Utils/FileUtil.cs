using System;
using System.IO;
using Serilog;

namespace Runaway.Utils;

public static class FileUtil
{
    public static string GetPluginDirectory()
    {
        var cwd = Directory.GetCurrentDirectory();
        return Path.Combine(cwd, "plugins");
    }

    public static bool DeleteAllFiles(string dirPath, bool recursive = false)
    {
        try
        {
            if (!Directory.Exists(dirPath))
            {
                Log.Error("目标目录不存�? {Dir}", dirPath);
                return false;
            }
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(dirPath, "*", option);
            var ok = true;
            foreach (var f in files)
            {
                try
                {
                    if (File.Exists(f))
                    {
                        var attr = File.GetAttributes(f);
                        if ((attr & System.IO.FileAttributes.ReadOnly) != 0)
                        {
                            File.SetAttributes(f, attr & ~System.IO.FileAttributes.ReadOnly);
                        }
                        File.Delete(f);
                    }
                }
                catch (Exception ex)
                {
                    ok = false;
                    Log.Error(ex, "删除文件失败: {File}", f);
                }
            }
            if (recursive)
            {
                var dirs = Directory.GetDirectories(dirPath, "*", SearchOption.AllDirectories);
                Array.Sort(dirs, (a, b) => b.Length.CompareTo(a.Length));
                foreach (var d in dirs)
                {
                    try
                    {
                        if (Directory.Exists(d))
                        {
                            var attr = File.GetAttributes(d);
                            if ((attr & System.IO.FileAttributes.ReadOnly) != 0)
                            {
                                File.SetAttributes(d, attr & ~System.IO.FileAttributes.ReadOnly);
                            }
                            Directory.Delete(d, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        ok = false;
                        Log.Error(ex, "删除目录失败: {Dir}", d);
                    }
                }
                try
                {
                    var rootAttr = File.GetAttributes(dirPath);
                    if ((rootAttr & System.IO.FileAttributes.ReadOnly) != 0)
                    {
                        File.SetAttributes(dirPath, rootAttr & ~System.IO.FileAttributes.ReadOnly);
                    }
                    Directory.Delete(dirPath, true);
                }
                catch (Exception ex)
                {
                    ok = false;
                    Log.Error(ex, "删除根目录失�? {Dir}", dirPath);
                }
            }
            return ok;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "删除目录内所有文件失�? {Dir}", dirPath);
            return false;
        }
    }
}

