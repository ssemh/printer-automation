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
            // Varsayılan bağlantı stringi - app.config'den alınır
            // Eğer app.config'de yoksa varsayılan olarak 27018 portunu kullanır
            _connectionString = ConfigurationManager.AppSettings["MongoDbConnectionString"] 
                ?? "mongodb://localhost:27018";
            
            _databaseName = ConfigurationManager.AppSettings["MongoDbDatabaseName"] 
                ?? "PrinterAutomation";

            try
            {
                System.Diagnostics.Debug.WriteLine($"[MongoDB] Bağlantı kuruluyor: {_connectionString}");
                System.Console.WriteLine($"[MongoDB] Bağlantı kuruluyor: {_connectionString}");
                
                var client = new MongoClient(_connectionString);
                _database = client.GetDatabase(_databaseName);
                
                // Bağlantıyı test et
                System.Diagnostics.Debug.WriteLine($"[MongoDB] Bağlantı test ediliyor...");
                _database.RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
                
                // Veritabanının oluşmasını garantilemek için collection listesini kontrol et
                try
                {
                    var collections = _database.ListCollectionNames().ToList();
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] Mevcut collection sayısı: {collections.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MongoDB] Collection listesi kontrolü hatası: {ex.Message}");
                }
                
                System.Diagnostics.Debug.WriteLine($"[MongoDB] ✓ Bağlantı başarılı: {_databaseName} @ {_connectionString}");
                System.Console.WriteLine($"[MongoDB] ✓ Bağlantı başarılı: {_databaseName} @ {_connectionString}");
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                var errorMsg = $"MongoDB sunucusuna bağlanılamıyor. MongoDB'nin {_connectionString} adresinde çalıştığından emin olun.\n\nHata: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[MongoDB] ✗ Socket hatası: {errorMsg}");
                System.Console.WriteLine($"[MongoDB] ✗ Socket hatası: {errorMsg}");
                throw new Exception(errorMsg, ex);
            }
            catch (MongoDB.Driver.MongoServerException ex)
            {
                var errorMsg = $"MongoDB sunucu hatası: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[MongoDB] ✗ Sunucu hatası: {errorMsg}");
                System.Console.WriteLine($"[MongoDB] ✗ Sunucu hatası: {errorMsg}");
                throw new Exception(errorMsg, ex);
            }
            catch (Exception ex)
            {
                var errorMsg = $"MongoDB bağlantı hatası: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[MongoDB] ✗ Genel hata: {errorMsg}");
                System.Diagnostics.Debug.WriteLine($"[MongoDB] StackTrace: {ex.StackTrace}");
                System.Console.WriteLine($"[MongoDB] ✗ Genel hata: {errorMsg}");
                throw new Exception(errorMsg, ex);
            }
        }

        public MongoDbService(string connectionString, string databaseName)
        {
            _connectionString = connectionString;
            _databaseName = databaseName;

            try
            {
                var client = new MongoClient(_connectionString);
                _database = client.GetDatabase(_databaseName);
                
                // Bağlantıyı test et
                _database.RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
                
                System.Diagnostics.Debug.WriteLine($"MongoDB bağlantısı başarılı: {_databaseName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MongoDB bağlantı hatası: {ex.Message}");
                throw;
            }
        }

        public IMongoDatabase Database => _database;

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName);
        }

        public bool IsConnected()
        {
            try
            {
                _database.RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ClearAllData()
        {
            if (_database == null)
            {
                System.Diagnostics.Debug.WriteLine($"[MongoDB] ⚠ Veritabanı bağlantısı yok - veriler temizlenemiyor");
                throw new InvalidOperationException("MongoDB veritabanı bağlantısı yok. Veriler temizlenemiyor.");
            }

            try
            {
                // Tüm collection'ları temizle
                var collections = _database.ListCollectionNames().ToList();
                foreach (var collectionName in collections)
                {
                    if (collectionName != "modelInfos") // Model bilgilerini koru
                    {
                        _database.DropCollection(collectionName);
                        System.Diagnostics.Debug.WriteLine($"[MongoDB] Collection silindi: {collectionName}");
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[MongoDB] Tüm veriler temizlendi (modelInfos hariç)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MongoDB] Veriler temizlenirken hata: {ex.Message}");
                throw;
            }
        }
    }
}
