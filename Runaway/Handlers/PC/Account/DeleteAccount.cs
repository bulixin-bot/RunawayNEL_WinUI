
using Runaway.Handlers.PC.Login;
using Runaway.Manager;

namespace Runaway.Handlers.PC.Account
{
    public class DeleteAccount
    {
        public object Execute(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                return new { type = "delete_error", message = "entityId为空" };
            }
            UserManager.Instance.RemoveAvailableUser(entityId);
            UserManager.Instance.RemoveUser(entityId);
            var items = GetAccount.GetAccountItems();
            return new { type = "accounts", items };
        }
    }
}

