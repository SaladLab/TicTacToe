using Domain.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using TrackableData.MongoDB;

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

        public IMongoCollection<BsonDocument> UserCollection => Database.GetCollection<BsonDocument>("User");

        public readonly static TrackableContainerMongoDbMapper<IUserContext> UserContextMapper =
            new TrackableContainerMongoDbMapper<IUserContext>();
    }
}
