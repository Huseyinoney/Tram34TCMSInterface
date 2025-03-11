using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class UdpSenderBackgroundService : BackgroundService
{
    private readonly string _targetIp = "100.10.133.33"; // Hedef cihazın IP adresi
    private readonly int _targetPort = 6000; // Hedef port
    private readonly UdpClient _udpClient;

    public UdpSenderBackgroundService()
    {
        _udpClient = new UdpClient(5005);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Veriyi sürekli olarak gönderecek
        while (!stoppingToken.IsCancellationRequested)
        {
            var data = new
            {
                TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                MasterTrainId = 123,
                TrainSpeed = 80,
                ZeroSpeed = false,
                TachoMeterPulse = true,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Time = DateTime.UtcNow.ToString("HH:mm:ss"),
                CouplingTrainsIds = new List<object>()
                {
                      new { CouplingTrainsIds = new List<int> { 1, 2, 3, 4 } }
                },

                TRAIN = new[]
                {
                    new
                    {
                        ID = 1,
                        IP = "192.168.1.1",
                        CabAActive = true,
                        CabBActive = false,
                        CabineAKeyStatus = true,
                        CabineBKeyStatus = false,
                        TrainCoupledOrder = 1,
                        IsTrainCoupled = true,
                        AllDoorOpen = true,
                        AllDoorClose = false
                    },
                    new
                    {
                        ID = 2,
                        IP = "192.168.1.2",
                        CabAActive = false,
                        CabBActive = true,
                        CabineAKeyStatus = false,
                        CabineBKeyStatus = true,
                        TrainCoupledOrder = 2,
                        IsTrainCoupled = false,
                        AllDoorOpen = false,
                        AllDoorClose = true
                    },
                    new
                    {
                        ID = 3,
                        IP = "192.168.1.3",
                        CabAActive = true,
                        CabBActive = true,
                        CabineAKeyStatus = true,
                        CabineBKeyStatus = false,
                        TrainCoupledOrder = 3,
                        IsTrainCoupled = true,
                        AllDoorOpen = true,
                        AllDoorClose = false
                    },
                    new
                    {
                        ID = 4,
                        IP = "192.168.1.4",
                        CabAActive = false,
                        CabBActive = true,
                        CabineAKeyStatus = false,
                        CabineBKeyStatus = true,
                        TrainCoupledOrder = 4,
                        IsTrainCoupled = false,
                        AllDoorOpen = false,
                        AllDoorClose = true
                    }
                }
            };

            // JSON verisini string'e dönüştür
            string jsonData = JsonSerializer.Serialize(data);

            // JSON verisini byte dizisine dönüştür
            byte[] sendBytes = Encoding.UTF8.GetBytes(jsonData);

            // Veriyi gönder
            await _udpClient.SendAsync(sendBytes, sendBytes.Length, _targetIp, _targetPort);

            Console.WriteLine($"Veri gönderildi: {jsonData}");

            // 10 saniye bekle, daha sonra veri göndermeye devam et
            await Task.Delay(500, stoppingToken); // 10 saniye gecikme
        }
    }

    public override void Dispose()
    {
        _udpClient?.Close();
        base.Dispose();
    }
}