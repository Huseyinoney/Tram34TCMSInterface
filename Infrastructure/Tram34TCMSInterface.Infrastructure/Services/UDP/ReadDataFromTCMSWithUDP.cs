using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tram34TCMSInterface.Application.Abstractions.CacheMemory;
using Tram34TCMSInterface.Application.Abstractions.LogService;
using Tram34TCMSInterface.Application.Abstractions.MongoDB;
using Tram34TCMSInterface.Application.Abstractions.UDP;
using Tram34TCMSInterface.Domain.Log;
using Tram34TCMSInterface.Infrastructure.RabbitMQ;
using static Tram34TCMSInterface.Domain.Models.JsonDocumentFormatUDP;

namespace Tram34TCMSInterface.Infrastructure.Services.UDP
{
    public class ReadDataFromTCMSWithUDP : IReadDataFromTCMSWithUDP
    {
        private readonly ILogService logService;
        private readonly ILogFactory logFactory;
        private readonly UdpClient udpClient;
        private readonly IConfiguration _configuration;
        private readonly IMongoDBTrainConfigurationCacheService mongoDBTrainConfigurationCacheService;
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private List<object> _previousTrainData = new(); // Önceki veriyi saklamak için

        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ReadDataFromTCMSWithUDP(IConfiguration configuration, IMongoDBTrainConfigurationCacheService mongoDBTrainConfigurationCacheService, ILogService logService, ILogFactory logFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            if (!int.TryParse(_configuration["UDP:Port"], out int port))
            {
                throw new ArgumentException("Geçersiz port numarası.");
            }

            udpClient = new UdpClient(port);
            this.mongoDBTrainConfigurationCacheService = mongoDBTrainConfigurationCacheService;
            this.logService = logService;
            this.logFactory = logFactory;
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

        // Önceki tren ve kuplaj bilgilerini saklamak için sınıf
        public class PreviousTrainState
        {
            public Train Train { get; set; }
            public List<string> CoupledIds { get; set; }
        }

        private PreviousTrainState previousTrainState;

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
                var currentTrain = data.TRAIN;

                if (!currentTrain.IsTrainCoupled)
                {
                    Console.WriteLine("Şu anki tren kuplajda değil.");
                    return false;
                }

                var coupledTrainIds = new[]
                {
            data.CouplingTrainsId.CouplingTrainsIdXX1,
            data.CouplingTrainsId.CouplingTrainsIdXX2,
            data.CouplingTrainsId.CouplingTrainsIdXX3,
            data.CouplingTrainsId.CouplingTrainsIdXX4
        }
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

                var resultWithMasterTrain = new
                {
                    MasterTrainId = masterTrainId,
                    CurrentTrain = new
                    {
                        currentTrain.ID,
                        currentTrain.IP,
                        currentTrain.TrainCoupledOrder,
                        currentTrain.IsTrainCoupled,
                        currentTrain.Cab_A_Active,
                        currentTrain.Cab_B_Active,
                        currentTrain.Cab_A_KeyStatus,
                        currentTrain.Cab_B_KeyStatus
                    },
                    CouplingTrainsIds = coupledTrainIds
                };

                string jsonOutput = JsonSerializer.Serialize(resultWithMasterTrain, jsonSerializerOptions);

                // Eski veri ile karşılaştırma
                bool isEqual = AreTrainsEqual(currentTrain, previousTrainState?.Train, coupledTrainIds, previousTrainState?.CoupledIds);

                if (!isEqual)
                {
                    Console.WriteLine($"Yeni veri gönderildi: {jsonOutput}");
                    var result = await RabbitMQService.PublishMessage(
                        RabbitMQConstant.RabbitMQHost,
                        RabbitMQConstant.CoupledTrainsExchangeName,
                        "fanout",
                        "",
                        jsonOutput,
                        ManagementEnum.Live
                    );

                    if (result)
                    {
                        mongoDBTrainConfigurationCacheService.SaveTrainInformationToCache(currentTrain.ID);
                        logService.TrainId = currentTrain.ID;
                         logService.SendLogAsync<EventLog>(
                            logFactory.CreateEventLog("Kuplaj Bilgisi TCMS'ten Alındı", "TCMSInterface", "", "", "")
                        );
                    }

                    // Yeni state’i kaydet
                    previousTrainState = new PreviousTrainState
                    {
                        Train = currentTrain,
                        CoupledIds = coupledTrainIds
                    };

                    return true;
                }
                else
                {
                    return false; // Veri değişmedi
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata oluştu: {ex.Message}");
                return false;
            }
        }

        // Tren ve kuplaj listesini karşılaştıran metod
        private bool AreTrainsEqual(Train currentTrain, Train previousTrain, List<string> currentCoupledIds, List<string> previousCoupledIds)
        {
            if (previousTrain == null) return false;

            bool trainEqual =
                currentTrain.ID == previousTrain.ID &&
                currentTrain.IP == previousTrain.IP &&
                currentTrain.TrainCoupledOrder == previousTrain.TrainCoupledOrder &&
                currentTrain.IsTrainCoupled == previousTrain.IsTrainCoupled &&
                currentTrain.Cab_A_Active == previousTrain.Cab_A_Active &&
                currentTrain.Cab_B_Active == previousTrain.Cab_B_Active &&
                currentTrain.Cab_A_KeyStatus == previousTrain.Cab_A_KeyStatus &&
                currentTrain.Cab_B_KeyStatus == previousTrain.Cab_B_KeyStatus;

            bool couplingEqual = Enumerable.SequenceEqual(currentCoupledIds ?? new List<string>(), previousCoupledIds ?? new List<string>());

            return trainEqual && couplingEqual;
        }





        //        public async Task<bool> SendCoupledDataToCoupleExchange(Tram34TCMSInterface.Domain.Models.JsonDocumentFormatUDP.TrainData data)
        //        {
        //            if (data == null)
        //            {
        //                Console.WriteLine("Geçersiz veri: Null veri alındı.");
        //                return false;
        //            }

        //            try
        //            {
        //                var masterTrainId = data.MasterTrainId;

        //                // Şu anki trenin bilgilerini alıyoruz.
        //                var currentTrain = data.TRAIN;  // Burada `TRAIN` zaten tek bir nesne olduğu için doğrudan erişim yapılır.

        //                // Eğer şu anki tren kuplajda değilse, işleme devam edilmez
        //                if (!currentTrain.IsTrainCoupled)
        //                {
        //                    Console.WriteLine("Şu anki tren kuplajda değil.");
        //                    return false;
        //                }


        //                var coupledTrainIds = new[]
        //{
        //                    data.CouplingTrainsId.CouplingTrainsIdXX1,
        //                    data.CouplingTrainsId.CouplingTrainsIdXX2,
        //                    data.CouplingTrainsId.CouplingTrainsIdXX3,
        //                    data.CouplingTrainsId.CouplingTrainsIdXX4
        //                }
        //            .Where(id => !string.IsNullOrEmpty(id))
        //            .ToList();

        //                //currentTrain.ID = "Train " + currentTrain.ID.ToString();
        //                // Şu anki trenin bilgilerini ve kuplajdaki trenlerin ID'lerini içeriyor
        //                var resultWithMasterTrain = new
        //                {
        //                    MasterTrainId = masterTrainId,
        //                    CurrentTrain = new
        //                    {
        //                        currentTrain.ID,  // Şu anki trenin ID'si
        //                        currentTrain.IP,  // Şu anki trenin IP'si
        //                        currentTrain.TrainCoupledOrder,// Kuplaj sırası
        //                        currentTrain.IsTrainCoupled,
        //                        currentTrain.Cab_A_Active,
        //                        currentTrain.Cab_B_Active,
        //                        currentTrain.Cab_A_KeyStatus,
        //                        currentTrain.Cab_B_KeyStatus
        //                    },
        //                    CouplingTrainsIds = coupledTrainIds  // Kuplajdaki trenlerin ID'leri
        //                };

        //                // JSON çıktısı oluşturma
        //                string jsonOutput = JsonSerializer.Serialize(resultWithMasterTrain, jsonSerializerOptions);

        //                // Eski veri ile karşılaştırma yapılması
        //                if (!_previousTrainData.Any() || !AreTrainsEqual(currentTrain, _previousTrainData.First() as Train))
        //                {
        //                    Console.WriteLine($"Yeni veri gönderildi: {jsonOutput}");
        //                    var result = await RabbitMQService.PublishMessage(RabbitMQConstant.RabbitMQHost, RabbitMQConstant.CoupledTrainsExchangeName, "fanout", "", jsonOutput, ManagementEnum.Live);
        //                    if (result)
        //                    {
        //                        mongoDBTrainConfigurationCacheService.SaveTrainInformationToCache(currentTrain.ID);
        //                        logService.TrainId = currentTrain.ID;
        //                        await logService.SendLogAsync<EventLog>(logFactory.CreateEventLog("Kuplaj Bilgisi TCMS'ten Alındı", "TCMSInterface", "", "", ""));
        //                    }


        //                    // Eski veriyi güncelle
        //                    _previousTrainData = new List<object> { currentTrain };

        //                    return true;  // Yeni veri gönderildi
        //                }
        //                else
        //                {
        //                    //Console.WriteLine("Veri değişmedi, gönderilmiyor.");
        //                    return false;  // Veri değişmedi
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"Hata oluştu: {ex.Message}");
        //                return false;  // Hata durumunda false döndür
        //            }
        //        }

        //        // Trenlerin eşitliğini kontrol eden yardımcı metod
        //        private bool AreTrainsEqual(Train currentTrain, Train previousTrain)
        //        {
        //            return currentTrain.ID == previousTrain.ID &&
        //                   currentTrain.IP == previousTrain.IP &&
        //                   currentTrain.TrainCoupledOrder == previousTrain.TrainCoupledOrder &&
        //                   currentTrain.Cab_A_Active == previousTrain.Cab_A_Active &&
        //                   currentTrain.Cab_B_Active == previousTrain.Cab_B_Active &&
        //                   currentTrain.Cab_A_KeyStatus == previousTrain.Cab_A_KeyStatus &&
        //                   currentTrain.Cab_B_KeyStatus == previousTrain.Cab_B_KeyStatus &&
        //                   currentTrain.IsTrainCoupled == previousTrain.IsTrainCoupled;
        //            //currentTrain.AllDoorOpen == previousTrain.AllDoorOpen &&
        //            //currentTrain.AllDoorClose == previousTrain.AllDoorClose &&
        //            //currentTrain.AllDoorReleased == previousTrain.AllDoorReleased &&
        //            //currentTrain.AllLeftDoorOpen == previousTrain.AllLeftDoorOpen &&
        //            //currentTrain.AllRightDoorOpen == previousTrain.AllRightDoorOpen &&
        //            //currentTrain.AllLeftDoorClose == previousTrain.AllLeftDoorClose &&
        //            //currentTrain.AllRightDoorClose == previousTrain.AllRightDoorClose &&
        //            //currentTrain.AllLeftDoorReleased == previousTrain.AllLeftDoorReleased &&
        //            //currentTrain.AllRightDoorReleased == previousTrain.AllRightDoorReleased;
        //        }


        public async Task<bool> SendTakoMeterPulseDataToTakoReadExchange(TrainData data)
        {

            try
            {
                await semaphoreSlim.WaitAsync();
                string pulseJson = JsonSerializer.Serialize(new
                {
                    TachoMeterPulse = data.TachoMeterPulse,
                    ZeroSpeed = data.ZeroSpeed,
                    TrainSpeed = data.TrainSpeed,
                    Doors = new
                    {
                        AllDoorOpen = data.TRAIN.AllDoorOpen,
                        AllDoorClose = data.TRAIN.AllDoorClose,
                        AllDoorReleased = data.TRAIN.AllDoorReleased,
                        AllLeftDoorOpen = data.TRAIN.AllLeftDoorOpen,
                        AllRightDoorOpen = data.TRAIN.AllRightDoorOpen,
                        AllLeftDoorClose = data.TRAIN.AllLeftDoorClose,
                        AllRightDoorClose = data.TRAIN.AllRightDoorClose,
                        AllLeftDoorReleased = data.TRAIN.AllLeftDoorReleased,
                        AllRightDoorReleased = data.TRAIN.AllRightDoorReleased
                    }
                }, jsonSerializerOptions);

                // RabbitMQ host listesi
                var rabbitHosts = new List<string>
        {
           _configuration["RabbitMQ:TrainHosts:0"],
           _configuration["RabbitMQ:TrainHosts:1"],
           _configuration["RabbitMQ:TrainHosts:2"],
           _configuration["RabbitMQ:TrainHosts:3"]
        };

                // Gönderim görevlerini oluştur
                var tasks = rabbitHosts.Select(async host =>
                await RabbitMQService.PublishMessage(
                        host,
                        RabbitMQConstant.TakoReadExchangeName,
                        "fanout",
                        "",
                        pulseJson,
                        ManagementEnum.Live

                    )
                );
                //Console.WriteLine(data.TachoMeterPulse);
                // Tüm görevleri aynı anda başlat ve bitene kadar bekle
                var result = await Task.WhenAll(tasks);



              //  Console.WriteLine($"Pulse verisi tüm trenlere eşzamanlı gönderildi.{data.TachoMeterPulse}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pulse gönderme hatası: {ex.Message}");
                return false;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        //public async Task<bool> SendTakoMeterPulseDataToTakoReadExchange(TrainData data)
        //{
        //    try
        //    {
        //        string pulseJson = JsonSerializer.Serialize(new
        //        {
        //            TachoMeterPulse = data.TachoMeterPulse,
        //            ZeroSpeed = data.ZeroSpeed,
        //            TrainSpeed = data.TrainSpeed,
        //            Doors = new
        //            {
        //                AllDoorOpen = data.TRAIN.AllDoorOpen,
        //                AllDoorClose = data.TRAIN.AllDoorClose,
        //                AllDoorReleased = data.TRAIN.AllDoorReleased,
        //                AllLeftDoorOpen = data.TRAIN.AllLeftDoorOpen,
        //                AllRightDoorOpen = data.TRAIN.AllRightDoorOpen,
        //                AllLeftDoorClose = data.TRAIN.AllLeftDoorClose,
        //                AllRightDoorClose = data.TRAIN.AllRightDoorClose,
        //                AllLeftDoorReleased = data.TRAIN.AllLeftDoorReleased,
        //                AllRightDoorReleased = data.TRAIN.AllRightDoorReleased
        //            }
        //        }, jsonSerializerOptions);

        //        // TachoMeterPulse'u JSON formatında serileştir

        //        // RabbitMQ kuyruğuna gönder
        //        await RabbitMQService.PublishMessage(
        //            RabbitMQConstant.RabbitMQHost,
        //            RabbitMQConstant.TakoReadExchangeName, // Pulse için yeni bir exchange adı
        //            "fanout", // Tüm abonelere göndermek için fanout exchange kullanabilirsiniz
        //            "", // Routing key'i boş bırakabilirsiniz
        //            pulseJson,  // JSON verisi
        //            ManagementEnum.Live // Verinin hangi türde olduğunu belirtebilirsiniz (örneğin: "Live")
        //        );

        //        Console.WriteLine($"Pulse değeri gönderildi: {pulseJson}\n");

        //        return true; // Başarılı
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Pulse gönderme hatası: {ex.Message}");
        //        return false; // Hata durumu
        //    }
        //}
    }
}