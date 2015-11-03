using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace GameServer
{
    public static class Authenticator
    {
        // Information for each user account.
        // Password will be stored as a plain-text because it's for demo only.
        private class AccountInfo
        {
            public string Id;
            public string Password;
            public DateTime LastLoginTime; 
        }

        // If account exists, check password correct.
        // Otherwise create new account with id and password.
        public static async Task<string> AuthenticateAsync(string id, string password)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            var accountCollection = MongoDbStorage.Instance.Database.GetCollection<AccountInfo>("Account");
            var account = await accountCollection.Find(a => a.Id == id).FirstOrDefaultAsync();
            if (account != null)
            {
                if (account.Password != password)
                    return string.Empty;

                account.LastLoginTime = DateTime.UtcNow;
            }
            else
            {
                account = new AccountInfo
                {
                    Id = id,
                    Password = password,
                    LastLoginTime = DateTime.UtcNow
                };
            }

            await accountCollection.ReplaceOneAsync(a => a.Id == id, account, new UpdateOptions { IsUpsert = true });
            return id;
        }
    }
}
