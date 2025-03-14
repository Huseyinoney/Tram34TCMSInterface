using Microsoft.Extensions.Configuration;
using RabbitMQ.Shared;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Tram34TCMSInterface.Application.Abstractions.UDP;

namespace Tram34TCMSInterface.Infrastructure.Services.UDP
{
    public class ReadDataFromTCMSWithUDP : IReadDataFromTCMSWithUDP
    {
        private UdpClient udpClient;
        private readonly IConfiguration _configuration;
        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        public ReadDataFromTCMSWithUDP(IConfiguration configuration)
        {
            _configuration = configuration;
            if (!int.TryParse(_configuration["UDP:Port"], out int port))
            {
                throw new Exception();
            }
            this.udpClient = CreateSocket(port);
        }

        private UdpClient CreateSocket(int Port)
        {
            return new UdpClient(Port);
        }

        public async Task<(byte[] Buffer, IPEndPoint SenderEndPoint)> ReadDataFromTCMS()
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                return (result.Buffer, result.RemoteEndPoint);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"UDP Socket Hatası: {ex.Message}");
                return (Array.Empty<byte>(), new IPEndPoint(IPAddress.None, 0));
            }
            finally { semaphoreSlim.Release(); }
        }

        public Domain.Models.JsonDocumentFormatUDP.TrainData ConvertByteArrayToJson(byte[] buffer)
        {
            try
            {
                // Byte dizisini UTF-8 ile string'e çevir
                string jsonString = Encoding.UTF8.GetString(buffer);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = false,  // JSON içindeki property isimleri case-sensitive olsun
                    AllowTrailingCommas = false,          // Fazladan virgül varsa hata versin
                    ReadCommentHandling = JsonCommentHandling.Disallow, // JSON içinde yorum (// veya /* */) varsa hata versin
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never // Gereksiz alanları yok saymasın, eksik alan varsa hata versin
                };
                // JSON string'ini JsonDocument'e dönüştür

                var data = JsonSerializer.Deserialize<Domain.Models.JsonDocumentFormatUDP.TrainData>(jsonString, options);

                // JSON dönüştürme başarılıysa JSON nesnesini döndür
                return data;
            }
            catch (JsonException ex)
            {
                // Geçersiz JSON verisi durumunda hata mesajı
                Console.WriteLine($"Geçersiz JSON Data: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Diğer hatalar için genel bir hata mesajı
                Console.WriteLine($"Beklenmeyen hata: {ex.Message}");
                return null;
            }
        }

        private List<object> _previousTrainData = new(); // Önceki veriyi saklamak için  
        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        public bool SendDataToLogicManager(Domain.Models.JsonDocumentFormatUDP.TrainData data)
        {
            try
            {
                // MasterTrainId'yi JSON'dan al
                var masterTrainId = data.MasterTrainId;  // MasterTrainId'yi JSON'dan alıyoruz

                var sortedTrains = data.TRAIN
                    .Where(train => train.IsTrainCoupled) // Sadece kuplajdaki trenleri al
                    .OrderBy(train => train.TrainCoupledOrder) // Kuplaj sırasına göre sırala
                    .Select(train => new
                    {
                        train.ID,
                        train.IP,
                        train.TrainCoupledOrder
                    })
                    .ToList();

                // MasterTrainId'yi de ekle
                var resultWithMasterTrain = new
                {
                    MasterTrainId = masterTrainId,  // MasterTrainId'yi ekliyoruz
                    Trains = sortedTrains
                };

                // Yeni gelen JSON verisini serialize et
                string jsonOutput = JsonSerializer.Serialize(resultWithMasterTrain, jsonSerializerOptions);

                // Yeni veri eski veriden farklı mı kontrol et
                if (!_previousTrainData.SequenceEqual(sortedTrains))
                {
                    Console.WriteLine($"Yeni veri gönderildi: {jsonOutput}");
                    RabbitMQHelper.PublishMessage(RabbitMQConstants.RabbitMQHost, RabbitMQConstants.LedExchangeName, "fanout", "", jsonOutput);

                    // Eski veriyi güncelle
                    _previousTrainData = new List<object>(sortedTrains);

                    return true; // Yeni veri gönderildiği için true döndür
                }
                else
                {
                    Console.WriteLine("Veri değişmedi, gönderilmiyor.");
                    return false; // Veri değişmediği için false döndür
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata oluştu: {ex.Message}");
                throw;
            }
        }
    }
}