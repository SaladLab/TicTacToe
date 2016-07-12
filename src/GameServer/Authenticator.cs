using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Driver;
using TrackableData.MongoDB;

namespace GameServer
{
    public static class Authenticator
    {
        // Information for each user account.
        public class AccountInfo
        {
            public string Id;
            public string PassSalt;
            public string PassHash;
            public long UserId;
            public DateTime RegisterTime;
            public DateTime LastLoginTime;
        }

        // If account exists, check password correct.
        // Otherwise create new account with id and password.
        public static async Task<AccountInfo> AuthenticateAsync(string id, string password)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var accountCollection = MongoDbStorage.Instance.Database.GetCollection<AccountInfo>("Account");
            await EnsureIndex(accountCollection);

            var account = await accountCollection.Find(a => a.Id == id).FirstOrDefaultAsync();
            if (account != null)
            {
                if (PasswordUtility.Verify(password, account.PassSalt, account.PassHash) == false)
                    return null;

                account.LastLoginTime = DateTime.UtcNow;
                await accountCollection.ReplaceOneAsync(a => a.Id == id, account);
            }
            else
            {
                var saltHash = PasswordUtility.CreateSaltHash(password);
                account = new AccountInfo
                {
                    Id = id,
                    PassSalt = saltHash.Item1,
                    PassHash = saltHash.Item2,
                    UserId = UniqueInt64Id.GenerateNewId(),
                    RegisterTime = DateTime.UtcNow,
                    LastLoginTime = DateTime.UtcNow
                };
                await accountCollection.InsertOneAsync(account);
            }

            return account;
        }

        private static bool s_indexEnsured;

        private static Task EnsureIndex(IMongoCollection<AccountInfo> collection)
        {
            if (s_indexEnsured)
                return Task.CompletedTask;

            s_indexEnsured = true;
            return collection.Indexes.CreateOneAsync(
                Builders<AccountInfo>.IndexKeys.Ascending(x => x.UserId),
                new CreateIndexOptions { Unique = true });
        }
    }

    // from http://geekswithblogs.net/Nettuce/archive/2012/06/14/salt-and-hash-a-password-in.net.aspx
    public static class PasswordUtility
    {
        public static Tuple<string, string> CreateSaltHash(string password)
        {
            var saltBytes = new byte[32];
            using (var provider = new RNGCryptoServiceProvider())
                provider.GetNonZeroBytes(saltBytes);
            var salt = Convert.ToBase64String(saltBytes);
            var hash = ComputeHash(password, salt);
            return Tuple.Create(salt, hash);
        }

        public static string ComputeHash(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            using (var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, saltBytes, 1000))
                return Convert.ToBase64String(rfc2898DeriveBytes.GetBytes(256));
        }

        public static bool Verify(string password, string salt, string hash)
        {
            return hash == ComputeHash(password, salt);
        }
    }
}
