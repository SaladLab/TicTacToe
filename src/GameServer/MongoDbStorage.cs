using Domain;
using MongoDB.Bson;
using MongoDB.Driver;
using TrackableData.MongoDB;

namespace GameServer
{
    public class MongoDbStorage
    {
        public static MongoDbStorage Instance { get; set; }

        public MongoDbStorage(string connectionString, string databaseName = "TicTacToe")
        {
            Client = new MongoClient(connectionString);
            DatabaseName = databaseName;
        }

        public MongoClient Client { get; }
        public string DatabaseName { get; }

        public IMongoDatabase Database => Client.GetDatabase(DatabaseName);
        public IMongoCollection<BsonDocument> UserCollection => Database.GetCollection<BsonDocument>("User");

        public readonly static TrackableContainerMongoDbMapper<IUserContext> UserContextMapper =
            new TrackableContainerMongoDbMapper<IUserContext>();
    }
}
