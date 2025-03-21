using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tram34TCMSInterface.Application.Abstractions.UDP;
using Tram34TCMSInterface.Infrastructure.RabbitMQ;
using static Tram34TCMSInterface.Domain.Models.JsonDocumentFormatUDP;

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
                    //DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,

                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
        //public async bool SendDataToLogicManager(Domain.Models.JsonDocumentFormatUDP.TrainData data)
        //{
        //    if (data == null)
        //    {
        //        Console.WriteLine("Geçersiz veri: Null veri alındı.");
        //        return false;
        //    }

        //    try
        //    {
        //        var masterTrainId = data.MasterTrainId;

        //        var sortedTrains = data.TRAIN
        //            .Where(train => train.IsTrainCoupled) // Sadece kuplajdaki trenleri al
        //            .OrderBy(train => train.TrainCoupledOrder) // Kuplaj sırasına göre sırala
        //            .Select(train => new
        //            {
        //                train.ID,
        //                train.IP,
        //                train.TrainCoupledOrder
        //            })
        //            .ToList();

        //        var resultWithMasterTrain = new
        //        {
        //            MasterTrainId = masterTrainId,
        //            Trains = sortedTrains
        //        };

        //        string jsonOutput = JsonSerializer.Serialize(resultWithMasterTrain, jsonSerializerOptions);

        //        // Eski veriye göre karşılaştırma yap
        //        if (!_previousTrainData.SequenceEqual(sortedTrains))
        //        {
        //            Console.WriteLine($"Yeni veri gönderildi: {jsonOutput}");
        //           await RabbitMQService.PublishMessage(RabbitMQConstant.RabbitMQHost, RabbitMQConstant.LedExchangeName, "fanout", "", jsonOutput,ManagementEnum.Live);

        //            // Eski veriyi güncelle
        //            _previousTrainData = new List<object>(sortedTrains);

        //            return true; // Yeni veri gönderildiği için true döndür
        //        }
        //        else
        //        {
        //            Console.WriteLine("Veri değişmedi, gönderilmiyor.");
        //            return false; // Veri değişmediği için false döndür
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Hata oluştu: {ex.Message}");
        //        return false; // Hata durumunda false döndür
        //    }
        //}

        //public async Task<bool> SendDataToLogicManager(Tram34TCMSInterface.Domain.Models.JsonDocumentFormatUDP.TrainData data)
        //{
        //    if (data == null)
        //    {
        //        Console.WriteLine("Geçersiz veri: Null veri alındı.");
        //        return false;
        //    }

        //    try
        //    {
        //        var masterTrainId = data.MasterTrainId;

        //        // Şu anki trenin bilgilerini alıyoruz.
        //        var currentTrain = data.TRAIN;  // Burada `TRAIN` zaten tek bir nesne olduğu için doğrudan erişim yapılır.

        //        // Eğer şu anki tren kuplajda değilse, işleme devam edilmez
        //        if (!currentTrain.IsTrainCoupled)
        //        {
        //            Console.WriteLine("Şu anki tren kuplajda değil.");
        //            return false;
        //        }

        //        // Kuplajdaki trenlerin ID'leri
        //        var coupledTrainIds = new List<int?>
        //{
        //    data.CouplingTrainsId.CouplingTrainsIdXX1,
        //    data.CouplingTrainsId.CouplingTrainsIdXX2,
        //    data.CouplingTrainsId.CouplingTrainsIdXX3,
        //    data.CouplingTrainsId.CouplingTrainsIdXXX
        //};

        //        // Şu anki trenin bilgilerini ve kuplajdaki trenlerin ID'lerini içeriyor
        //        var resultWithMasterTrain = new
        //        {
        //            MasterTrainId = masterTrainId,
        //            CurrentTrain = new
        //            {
        //                currentTrain.ID,  // Şu anki trenin ID'si
        //                currentTrain.IP,  // Şu anki trenin IP'si
        //                currentTrain.TrainCoupledOrder  // Kuplaj sırası
        //            },
        //            CouplingTrainsIds = coupledTrainIds  // Kuplajdaki trenlerin ID'leri
        //        };

        //        // JSON çıktısı oluşturma
        //        string jsonOutput = JsonSerializer.Serialize(resultWithMasterTrain, jsonSerializerOptions);

        //        // Eski veri ile karşılaştırma yapılması
        //        if (!_previousTrainData.SequenceEqual(new List<object> { currentTrain }))
        //        {
        //            Console.WriteLine($"Yeni veri gönderildi: {jsonOutput}");
        //            await RabbitMQService.PublishMessage(RabbitMQConstant.RabbitMQHost, RabbitMQConstant.LedExchangeName, "fanout", "", jsonOutput, ManagementEnum.Live);

        //            // Eski veriyi güncelle
        //            _previousTrainData = new List<object> { currentTrain };

        //            return true;  // Yeni veri gönderildi
        //        }
        //        else
        //        {
        //            Console.WriteLine("Veri değişmedi, gönderilmiyor.");
        //            return false;  // Veri değişmedi
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Hata oluştu: {ex.Message}");
        //        return false;  // Hata durumunda false döndür
        //    }
        //}


        public async Task<bool> SendCoupledDataToCoupleExchange(Tram34TCMSInterface.Domain.Models.JsonDocumentFormatUDP.TrainData data)
        {
            if (data == null)
            {
                Console.WriteLine("Geçersiz veri: Null veri alındı.");
                return false;
            }

            try
            {
                var masterTrainId = data.MasterTrainId;

                // Şu anki trenin bilgilerini alıyoruz.
                var currentTrain = data.TRAIN;  // Burada `TRAIN` zaten tek bir nesne olduğu için doğrudan erişim yapılır.

                // Eğer şu anki tren kuplajda değilse, işleme devam edilmez
                if (!currentTrain.IsTrainCoupled)
                {
                    Console.WriteLine("Şu anki tren kuplajda değil.");
                    return false;
                }

                // Kuplajdaki trenlerin ID'leri
                var coupledTrainIds = new List<int?>
        {
            data.CouplingTrainsId.CouplingTrainsIdXX1,
            data.CouplingTrainsId.CouplingTrainsIdXX2,
            data.CouplingTrainsId.CouplingTrainsIdXX3,
            data.CouplingTrainsId.CouplingTrainsIdXXX
        };

                // Şu anki trenin bilgilerini ve kuplajdaki trenlerin ID'lerini içeriyor
                var resultWithMasterTrain = new
                {
                    MasterTrainId = masterTrainId,
                    CurrentTrain = new
                    {
                        currentTrain.ID,  // Şu anki trenin ID'si
                        currentTrain.IP,  // Şu anki trenin IP'si
                        currentTrain.TrainCoupledOrder  // Kuplaj sırası
                    },
                    CouplingTrainsIds = coupledTrainIds  // Kuplajdaki trenlerin ID'leri
                };

                // JSON çıktısı oluşturma
                string jsonOutput = JsonSerializer.Serialize(resultWithMasterTrain, jsonSerializerOptions);

                // Eski veri ile karşılaştırma yapılması
                if (!_previousTrainData.Any() || !AreTrainsEqual(currentTrain, _previousTrainData.First() as Train))
                {
                    Console.WriteLine($"Yeni veri gönderildi: {jsonOutput}");
                    await RabbitMQService.PublishMessage(RabbitMQConstant.RabbitMQHost, RabbitMQConstant.LedExchangeName, "fanout", "", jsonOutput, ManagementEnum.LastMessage);

                    // Eski veriyi güncelle
                    _previousTrainData = new List<object> { currentTrain };

                    return true;  // Yeni veri gönderildi
                }
                else
                {
                    Console.WriteLine("Veri değişmedi, gönderilmiyor.");
                    return false;  // Veri değişmedi
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata oluştu: {ex.Message}");
                return false;  // Hata durumunda false döndür
            }
        }

        // Trenlerin eşitliğini kontrol eden yardımcı metod
        private bool AreTrainsEqual(Train currentTrain, Train previousTrain)
        {
            return currentTrain.ID == previousTrain.ID &&
                   currentTrain.IP == previousTrain.IP &&
                   currentTrain.TrainCoupledOrder == previousTrain.TrainCoupledOrder &&
                   currentTrain.Cab_A_Active == previousTrain.Cab_A_Active &&
                   currentTrain.Cab_B_Active == previousTrain.Cab_B_Active &&
                   currentTrain.Cab_A_KeyStatus == previousTrain.Cab_A_KeyStatus &&
                   currentTrain.Cab_B_KeyStatus == previousTrain.Cab_B_KeyStatus &&
                   currentTrain.IsTrainCoupled == previousTrain.IsTrainCoupled &&
                   currentTrain.AllDoorOpen == previousTrain.AllDoorOpen &&
                   currentTrain.AllDoorClose == previousTrain.AllDoorClose &&
                   currentTrain.AllDoorReleased == previousTrain.AllDoorReleased &&
                   currentTrain.AllLeftDoorOpen == previousTrain.AllLeftDoorOpen &&
                   currentTrain.AllRightDoorOpen == previousTrain.AllRightDoorOpen &&
                   currentTrain.AllLeftDoorClose == previousTrain.AllLeftDoorClose &&
                   currentTrain.AllRightDoorClose == previousTrain.AllRightDoorClose &&
                   currentTrain.AllLeftDoorReleased == previousTrain.AllLeftDoorReleased &&
                   currentTrain.AllRightDoorReleased == previousTrain.AllRightDoorReleased;
        }

        public async Task<bool> SendTakoMeterPulseDataToTakoReadExchange(TrainData data)
        {
            try
            {
                // TachoMeterPulse değerini al
                bool tachoMeterPulse = data.TachoMeterPulse;

                // TachoMeterPulse'u JSON formatında serileştir
                string pulseJson = JsonSerializer.Serialize(new { TachoMeterPulse = tachoMeterPulse });

                // RabbitMQ kuyruğuna gönder
                await RabbitMQService.PublishMessage(
                    RabbitMQConstant.RabbitMQHost,
                    RabbitMQConstant.TakoReadExchangeName, // Pulse için yeni bir exchange adı
                    "fanout", // Tüm abonelere göndermek için fanout exchange kullanabilirsiniz
                    "", // Routing key'i boş bırakabilirsiniz
                    pulseJson,  // JSON verisi
                    ManagementEnum.Live // Verinin hangi türde olduğunu belirtebilirsiniz (örneğin: "Live")
                );

                Console.WriteLine($"Pulse değeri gönderildi: {pulseJson}");

                return true; // Başarılı
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pulse gönderme hatası: {ex.Message}");
                return false; // Hata durumu
            }
        }
    }
}