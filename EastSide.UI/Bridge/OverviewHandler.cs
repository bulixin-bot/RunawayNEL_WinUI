using Runaway.Manager;
using System.Linq;

namespace Runaway.UI.Bridge;

public static class OverviewHandler
{
    public static BridgeResponse GetRecent(BridgeRequest req)
    {
        var items = RecentPlayManager.Instance.GetAll().Select(e => new
        {
            serverId = e.ServerId,
            serverName = e.ServerName,
            type = e.Type,
            playTime = e.PlayTime.ToString("o"),
            mcVersion = e.McVersion,
            hasPassword = e.HasPassword
        }).ToList();

        return BridgeResponse.Ok(req, new { items });
    }

    public static BridgeResponse GetGameDuration(BridgeRequest req)
    {
        // 这里可以从游戏会话管理器或其他地方获取游戏时长
        // 暂时返回一个模拟值
        var totalMinutes = 125; // 示例值：2小时5分钟

        return BridgeResponse.Ok(req, new { totalMinutes });
    }
}

