using System;
using System.Configuration;
using MongoDB.Driver;

namespace GameServer.Tests
{
    public class MongoDbStorageFixture : IDisposable
    {
        public MongoDbStorageFixture()
        {
            var cstr = ConfigurationManager.ConnectionStrings["Mongo"].ConnectionString;
            MongoDbStorage.Instance = new MongoDbStorage(cstr, "Test");
            MongoDbStorage.Instance.Client.DropDatabaseAsync(MongoDbStorage.Instance.DatabaseName).Wait();
        }

        public void Dispose()
        {
            MongoDbStorage.Instance = null;
        }
    }
}
