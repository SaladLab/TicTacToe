using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace GameServer
{
    public class MongoDbStorage
    {
        public static MongoDbStorage Instance { get; set; }

        public MongoDbStorage(string connectionString)
        {
            Client = new MongoClient(connectionString);
        }

        public MongoClient Client { get; }

        public IMongoDatabase Database => Client.GetDatabase("TicTacToe");
    }
}
