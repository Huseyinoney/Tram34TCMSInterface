using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using Tram34TCMSInterface.Application.Abstractions.CacheMemory;
using Tram34TCMSInterface.Application.Abstractions.LogService;
using Tram34TCMSInterface.Application.Abstractions.TCP;
using Tram34TCMSInterface.Domain.Log;
using Tram34TCMSInterface.Infrastructure.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Tram34TCMSInterface.Application.Abstractions.Common;

namespace Tram34TCMSInterface.Infrastructure.Services.TCP
{
    public class ReadDataFromTCMSWithTCP : IReadDataFromTCMSWithTCP
    {
        private readonly IRabbitService rabbitService;
        private readonly ILogService logService;
        private readonly ILogFactory logFactory;
        private readonly IMongoDBTrainConfigurationCacheService mongoDBTrainConfigurationCacheService;
        private readonly IConfiguration configuration;
        private readonly ITrainContext trainContext;


        // private string? TrainIP;


        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ReadDataFromTCMSWithTCP(IMongoDBTrainConfigurationCacheService mongoDBTrainConfigurationCacheService, ILogService logService, ILogFactory logFactory, IConfiguration configuration, IRabbitService rabbitService, ITrainContext trainContext)
        {

            this.mongoDBTrainConfigurationCacheService = mongoDBTrainConfigurationCacheService;
            this.logService = logService;
            this.logFactory = logFactory;
            this.configuration = configuration;
            this.rabbitService = rabbitService;
            this.trainContext = trainContext;
        }

        //bu method kullanım dışı
        public async Task<byte[]> ReadDataFromTCMS(NetworkStream stream)
        {
            byte[] buffer = new byte[1500];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            return buffer[..bytesRead];
        }

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
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                // Console.WriteLine(jsonString);
                // return JsonSerializer.Deserialize<TrainData>(jsonString, options);
                var data = JsonSerializer.Deserialize<Domain.Models.JsonDocumentFormatUDP.TrainData>(jsonString, options);
                string jsonOutput = JsonSerializer.Serialize(data, jsonSerializerOptions);

                Console.WriteLine(jsonOutput);

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                // logService.SendLogAsync<ErrorLog>(logFactory.CreateErrorLog($"{ex.Message}", configuration["Log:TCMSSource"], "Software",""));
                return null;
            }
        }

        private List<object> _previousTrainData = new();
        private List<string> _previousCoupledTrainIds = new();
        private string _previousMasterTrainId = string.Empty;

        public async Task<bool> SendCoupledDataToCoupleExchange(Domain.Models.JsonDocumentFormatUDP.TrainData data)
        {
            if (data == null /*|| !data.TRAIN.IsTrainCoupled*/)
                return false;

            var masterTrainId = data.MasterTrainId;
            var currentTrain = data.TRAIN;

            var coupledTrainIds = new[]
            {
                data.CouplingTrainsId.CouplingTrainsIdXX1,
                data.CouplingTrainsId.CouplingTrainsIdXX2,
                data.CouplingTrainsId.CouplingTrainsIdXX3,
                data.CouplingTrainsId.CouplingTrainsIdXX4
            }.Where(id => !string.IsNullOrEmpty(id)).ToList();

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

            bool isTrainChanged = !_previousTrainData.Any() || !AreTrainsEqual(currentTrain, _previousTrainData.First() as Domain.Models.JsonDocumentFormatUDP.Train);
            bool isCoupledTrainIdsChanged = !_previousCoupledTrainIds.Any() || !AreCoupledTrainIdsEqual(coupledTrainIds, _previousCoupledTrainIds);
            bool isMasterTrainIdChanged = _previousMasterTrainId != masterTrainId;

            if (isTrainChanged || isCoupledTrainIdsChanged || isMasterTrainIdChanged)
            {
                var result = await rabbitService.PublishMessage(
                    RabbitMQConstant.RabbitMQHost,
                    RabbitMQConstant.CoupledTrainsExchangeName,
                    "fanout",
                    "",
                    jsonOutput,
                    ManagementEnum.Live);

                if (result)
                {
                    Console.WriteLine($" \nYeni veri gönderildi: \n {jsonOutput}\n");
                    mongoDBTrainConfigurationCacheService.SaveTrainInformationToCache(currentTrain.ID);

                    trainContext.TrainId = currentTrain.ID;

                    trainContext.TrainIP = mongoDBTrainConfigurationCacheService.GetHardware(currentTrain.ID);

                    if (Convert.ToBoolean(configuration["LogStatus:Event"]))
                    {

                        logService.SendLogAsync<EventLog>(logFactory.CreateEventLog($" {coupledTrainIds} Kuplaj Bilgisi TCMS'ten Alındı", configuration["Log:TCMSSource"], trainContext.TrainIP, configuration["Log:TCMSSource"], trainContext.TrainIP));
                    }
                }

                _previousTrainData = new List<object> { currentTrain };
                _previousCoupledTrainIds = new List<string>(coupledTrainIds);
                _previousMasterTrainId = masterTrainId;

                return true;
            }

            return false;
        }

        private bool AreCoupledTrainIdsEqual(List<string> currentIds, List<string> previousIds)
        {
            return currentIds.Count == previousIds.Count && currentIds.OrderBy(x => x).SequenceEqual(previousIds.OrderBy(x => x));
        }

        private bool AreTrainsEqual(Domain.Models.JsonDocumentFormatUDP.Train a, Domain.Models.JsonDocumentFormatUDP.Train b)
        {
            return a.ID == b.ID &&
                   a.IP == b.IP &&
                   a.TrainCoupledOrder == b.TrainCoupledOrder &&
                   a.Cab_A_Active == b.Cab_A_Active &&
                   a.Cab_B_Active == b.Cab_B_Active &&
                   a.Cab_A_KeyStatus == b.Cab_A_KeyStatus &&
                   a.Cab_B_KeyStatus == b.Cab_B_KeyStatus &&
                   a.IsTrainCoupled == b.IsTrainCoupled;
        }

        public async Task<bool> SendTakoMeterPulseDataToTakoReadExchange(Domain.Models.JsonDocumentFormatUDP.TrainData data)
        {
            try
            {
                string pulseJson = JsonSerializer.Serialize(new
                {
                    data.TachoMeterPulse,
                    data.ZeroSpeed,
                    data.TrainSpeed,
                    Doors = new
                    {
                        data.TRAIN.AllDoorOpen,
                        data.TRAIN.AllDoorClose,
                        data.TRAIN.AllDoorReleased,
                        data.TRAIN.AllLeftDoorOpen,
                        data.TRAIN.AllRightDoorOpen,
                        data.TRAIN.AllLeftDoorClose,
                        data.TRAIN.AllRightDoorClose,
                        data.TRAIN.AllLeftDoorReleased,
                        data.TRAIN.AllRightDoorReleased
                    }
                }, jsonSerializerOptions);

                await rabbitService.PublishMessage(
                    RabbitMQConstant.RabbitMQHost,
                    RabbitMQConstant.TakoReadExchangeName,
                    "fanout",
                    "",
                    pulseJson,
                    ManagementEnum.Live);
                Console.WriteLine($"\nPulse değeri gönderildi: {pulseJson}\n");
                if (Convert.ToBoolean(configuration["LogStatus:Event"]))
                {

                    logService.SendLogAsync<EventLog>(logFactory.CreateEventLog($"Pulse Değeri {data.TachoMeterPulse} TakoRead Kuyruğuna Gönderildi", configuration["Log:TCMSSource"], trainContext.TrainIP, configuration["Log:TCMSSource"], trainContext.TrainIP));
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}