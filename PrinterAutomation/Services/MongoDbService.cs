using MongoDB.Driver;
using System;
using System.Configuration;
using System.Linq;

namespace PrinterAutomation.Services
{
    public class MongoDbService
    {
        private readonly IMongoDatabase _database;
        private readonly string _connectionString;
        private readonly string _databaseName;

        public MongoDbService()
        {
            _connectionString = ConfigurationManager.AppSettings["MongoDbConnectionString"] ?? "mongodb://localhost:27018";
            _databaseName = ConfigurationManager.AppSettings["MongoDbDatabaseName"] ?? "PrinterAutomation";

            try
            {
                System.Diagnostics.Debug.WriteLine($"[MongoDB] Bağlantı kuruluyor: {_connectionString}");
                var client = new MongoClient(_connectionString);
                _database = client.GetDatabase(_databaseName);
                _database.RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
                var collections = _database.ListCollectionNames().ToList();
                System.Diagnostics.Debug.WriteLine($"[MongoDB]  Bağlantı başarılı: {_databaseName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MongoDB]  Hata: {ex.Message}");
                throw;
            }
        }

        public IMongoDatabase Database => _database;
        public IMongoCollection<T> GetCollection<T>(string collectionName) => _database.GetCollection<T>(collectionName);
        public bool IsConnected()
        {
            try { _database.RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1)); return true; }
            catch { return false; }
        }
    }
}
