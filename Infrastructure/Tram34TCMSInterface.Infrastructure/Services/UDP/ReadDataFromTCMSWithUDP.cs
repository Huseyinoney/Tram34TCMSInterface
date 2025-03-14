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
        private readonly UdpClient udpClient;
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private List<object> _previousTrainData = new(); // Önceki veriyi saklamak için
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ReadDataFromTCMSWithUDP(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            if (!int.TryParse(_configuration["UDP:Port"], out int port))
            {
                throw new ArgumentException("Geçersiz port numarası.");
            }

            udpClient = new UdpClient(port);
        }

        // Asenkron veri okuma işlemi
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
            catch (Exception ex)
            {
                Console.WriteLine($"Beklenmeyen hata: {ex.Message}");
                return (Array.Empty<byte>(), new IPEndPoint(IPAddress.None, 0));
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        // Byte dizisini JSON formatına dönüştürme
        public Domain.Models.JsonDocumentFormatUDP.TrainData ConvertByteArrayToJson(byte[] buffer)
        {
            try
            {
                string jsonString = Encoding.UTF8.GetString(buffer);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = false,
                    AllowTrailingCommas = false,
                    ReadCommentHandling = JsonCommentHandling.Disallow,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
                };

                var data = JsonSerializer.Deserialize<Domain.Models.JsonDocumentFormatUDP.TrainData>(jsonString, options);
                return data;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Geçersiz JSON Data: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Beklenmeyen hata: {ex.Message}");
                return null;
            }
        }

        // Veriyi Logic Manager'a gönderme
        public bool SendDataToLogicManager(Domain.Models.JsonDocumentFormatUDP.TrainData data)
        {
            if (data == null)
            {
                Console.WriteLine("Geçersiz veri: Null veri alındı.");
                return false;
            }

            try
            {
                var masterTrainId = data.MasterTrainId;

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

                var resultWithMasterTrain = new
                {
                    MasterTrainId = masterTrainId,
                    Trains = sortedTrains
                };

                string jsonOutput = JsonSerializer.Serialize(resultWithMasterTrain, jsonSerializerOptions);

                // Eski veriye göre karşılaştırma yap
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
                return false; // Hata durumunda false döndür
            }
        }
    }
}