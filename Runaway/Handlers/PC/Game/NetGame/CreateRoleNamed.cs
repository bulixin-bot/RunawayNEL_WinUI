using System;
using System.Linq;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Runaway.Type;
using Runaway.Entities.Web.NetGame;
using Serilog;
using Runaway.Manager;

namespace Runaway.Handlers.PC.Game.NetGame;

public class CreateRoleNamed
{
    public ServerRolesResult Execute(string serverId, string name)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new ServerRolesResult { NotLogin = true };
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(name))
        {
            return new ServerRolesResult { Success = false, Message = "参数错误" };
        }
        try
        {
            AppState.X19.CreateCharacter(last.UserId, last.AccessToken, serverId, name);
            if (SettingManager.Instance.Get().Debug) Log.Information("角色创建成功: serverId={ServerId}, name={Name}", serverId, name);
            Codexus.Cipher.Entities.Entities<EntityGameCharacter> entities = AppState.X19.QueryNetGameCharacters(last.UserId, last.AccessToken, serverId);
            var items = entities.Data.Select(r => new RoleItem { Id = r.Name, Name = r.Name }).ToList();
            return new ServerRolesResult { Success = true, ServerId = serverId, Items = items };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "角色创建失败: serverId={ServerId}, name={Name}", serverId, name);
            return new ServerRolesResult { Success = false, Message = ex.Message };
        }
    }

    public ServerRolesResult ExecuteForAccount(string accountId, string serverId, string name)
    {
        if (string.IsNullOrWhiteSpace(accountId)) return new ServerRolesResult { Success = false, Message = "参数错误" };
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(name))
        {
            return new ServerRolesResult { Success = false, Message = "参数错误" };
        }
        try
        {
            var u = UserManager.Instance.GetAvailableUser(accountId);
            if (u == null) return new ServerRolesResult { NotLogin = true };
            AppState.X19.CreateCharacter(u.UserId, u.AccessToken, serverId, name);
            if (SettingManager.Instance.Get().Debug) Log.Information("角色创建成功: serverId={ServerId}, name={Name}, account={AccountId}", serverId, name, accountId);
            Codexus.Cipher.Entities.Entities<EntityGameCharacter> entities = AppState.X19.QueryNetGameCharacters(u.UserId, u.AccessToken, serverId);
            var items = entities.Data.Select(r => new RoleItem { Id = r.Name, Name = r.Name }).ToList();
            return new ServerRolesResult { Success = true, ServerId = serverId, Items = items };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "角色创建失败: serverId={ServerId}, name={Name}, account={AccountId}", serverId, name, accountId);
            return new ServerRolesResult { Success = false, Message = ex.Message };
        }
    }
}

