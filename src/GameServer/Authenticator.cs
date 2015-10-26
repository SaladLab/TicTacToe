using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameServer
{
    public static class Authenticator
    {
        // Information for each user account.
        // Password will be stored as a plain-text because it's for demo only.
        private class AccountInfo
        {
            public string Password;
            public DateTime LastLoginTime; 
        }

        // If account exists, check password correct.
        // Otherwise create new account with id and password.
        public static async Task<bool> AuthenticateAsync(string id, string password)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            return true;
            /*
            AccountInfo account;

            var data = await RedisStorage.Db.HashGetAsync("Accounts", id);
            if (data.HasValue)
            {
                try
                {
                    account = JsonConvert.DeserializeObject<AccountInfo>(data.ToString());
                    if (account.Password != password)
                        return false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
                account.LastLoginTime = DateTime.UtcNow;
            }
            else
            {
                account = new AccountInfo
                {
                    Password = password,
                    LastLoginTime = DateTime.UtcNow
                };
            }

            var dataNew = JsonConvert.SerializeObject(account);
            await RedisStorage.Db.HashSetAsync("Accounts", id, dataNew);
            return true;
            */
        }
    }
}
