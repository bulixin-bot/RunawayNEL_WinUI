using System;
using System.Collections.Generic;
using System.Linq;
using Runaway.Manager;
using Runaway.Type;
using Runaway.Entities.Web.NetGame;
using Serilog;

namespace Runaway.Handlers.PC.Game.NetGame
{
    public class GetServersDetail
    {
        public ServerDetailResult Execute(string gameId)
        {
            var last = UserManager.Instance.GetLastAvailableUser();
            if (last == null) return new ServerDetailResult { NotLogin = true };
            if (string.IsNullOrWhiteSpace(gameId)) return new ServerDetailResult { Success = false, Message = "参数错误" };
            try
            {
                var detail = AppState.X19.QueryNetGameDetailById(last.UserId, last.AccessToken, gameId);
                var imgs = new List<string>();
                var desc = string.Empty;
                if (detail?.Data != null)
                {
                    if (detail.Data.BriefImageUrls != null)
                    {
                        imgs = detail.Data.BriefImageUrls
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(s => s.Replace("`", string.Empty).Trim())
                            .ToList();
                    }
                    desc = detail.Data.DetailDescription ?? string.Empty;
                }
                return new ServerDetailResult { Success = true, Images = imgs, Description = desc };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取服务器详情失�? {GameId}", gameId);
                return new ServerDetailResult { Success = false, Message = "获取失败" };
            }
        }
    }
}

