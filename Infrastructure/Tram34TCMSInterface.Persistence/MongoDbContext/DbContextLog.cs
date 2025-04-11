using MongoDB.Driver;
using System.Net.Sockets;
using Tram34TCMSInterface.Domain.Log;

namespace Tram34TCMSInterface.Persistence.MongoDBContext
{
    public class DBContextLog
    {
        private static IMongoClient _client;
        private readonly IMongoDatabase _database;
        private static bool _isMongoConnected = false;
        private const int RetryDelayMilliseconds = 5000; // 5 saniye bekleme süresi

        public DBContextLog(string connectionString, string databaseName)
        {
            _client ??= new MongoClient(connectionString); // Eğer _client null ise oluştur
            _database = _client.GetDatabase(databaseName);

            while (!_isMongoConnected) // Bağlantı başarılı olana kadar dene
            {
                try
                {
                    Console.WriteLine("MongoDB'ye bağlanılıyor...");

                    // MongoDB'ye erişimi test et
                    _database.ListCollectionNames(); // Eğer bağlantı başarısızsa hata fırlatır

                    _isMongoConnected = true; // Bağlantı başarılı olursa çık
                    Console.WriteLine("MongoDB bağlantısı başarılı.");
                }
                catch (MongoConnectionException ex)
                {
                    Console.WriteLine($"MongoDB bağlantı hatası: {ex.Message}");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Socket hatası: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Genel hata: {ex.Message}");
                }

                if (!_isMongoConnected)
                {
                    Console.WriteLine($"Bağlantı başarısız! {RetryDelayMilliseconds / 1000} saniye sonra tekrar denenecek...");
                    Thread.Sleep(RetryDelayMilliseconds); // 5 saniye bekle, tekrar dene
                }
            }
        }

        public IMongoCollection<TrainConfiguration> Log
        {
            get
            {
                if (!_isMongoConnected)
                {
                    throw new InvalidOperationException("MongoDB bağlantısı kurulamadı.");
                }
                return _database.GetCollection<TrainConfiguration>("TrainDB");
            }
        }
    }
}
