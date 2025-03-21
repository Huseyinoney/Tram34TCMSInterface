//using Microsoft.Extensions.Hosting;
//using System.Net.Sockets;
//using System.Text;
//using System.Text.Json;

//public class UdpSenderBackgroundService : BackgroundService
//{
//    private readonly string _targetIp = "100.10.133.33"; // Hedef cihazın IP adresi
//    private readonly int _targetPort = 6000; // Hedef port
//    private readonly UdpClient _udpClient;

//    public UdpSenderBackgroundService()
//    {
//        _udpClient = new UdpClient(5005);
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            var train =

//                new
//                {
//                    ID = 202,
//                    IP = "192.168.1.100",
//                    CabAActive = true,
//                    CabBActive = false,
//                    CabineAKeyStatus = true,
//                    CabineBKeyStatus = false,
//                    TrainCoupledOrder = 2,
//                    IsTrainCoupled = true,
//                    AllDoorOpen = false,
//                    AllDoorClose = true,
//                    AllDoorReleased = false,
//                    AllLeftDoorOpen = false,
//                    AllRightDoorOpen = true,
//                    AllLeftDoorClose = true,
//                    AllRightDoorClose = false,
//                    AllLeftDoorReleased = false,
//                    AllRightDoorReleased = true
//                };


//            var data = new
//            {
//                TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
//                MasterTrainId = train.ID,
//                TrainSpeed = 45.6,
//                ZeroSpeed = false,
//                TachoMeterPulse = true,
//                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
//                Time = DateTime.UtcNow.ToString("HH:mm:ss"),
//                CouplingTrainsId = new
//                {
//                    CouplingTrainsIdXX1 = 201,
//                    CouplingTrainsIdXX2 = 202,
//                    CouplingTrainsIdXX3 = 203,
//                    CouplingTrainsIdXXX = 204
//                },
//                Train = train
//            };

//            var options = new JsonSerializerOptions
//            {
//                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//            };

//            string jsonData = JsonSerializer.Serialize(data, options);
//            byte[] sendBytes = Encoding.UTF8.GetBytes(jsonData);
//            await _udpClient.SendAsync(sendBytes, sendBytes.Length, _targetIp, _targetPort);

//            Console.WriteLine($"Veri gönderildi: {jsonData}");
//            await Task.Delay(500, stoppingToken);
//        }
//    }

//    public override void Dispose()
//    {
//        _udpClient?.Close();
//        base.Dispose();
//    }
//}

using Microsoft.Extensions.Hosting;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

public class UdpSenderBackgroundService : BackgroundService
{
    private readonly string _targetIp = "100.10.133.33"; // Hedef cihazın IP adresi
    private readonly int _targetPort = 6000; // Hedef port
    private readonly UdpClient _udpClient;
    private bool _isPulseActive = false;  // Pulse durumunu takip etmek için bir değişken
    private bool _isWKeyPressed = false;  // W tuşunun basılı olup olmadığını kontrol etmek için bir değişken

    public UdpSenderBackgroundService()
    {
        _udpClient = new UdpClient(5005);
    }

    // BackgroundService'de veri gönderme işlemini sürekli yapıyoruz
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // W tuşuna basılıp basılmadığını kontrol etmek için sürekli bir döngü çalıştırıyoruz
        Task.Run(() => MonitorKeyPress());
        while (!stoppingToken.IsCancellationRequested)
        {

            // Eğer pulse aktifse, true olarak gönderiyoruz
            var tachoMeterPulse = _isPulseActive;

            var train = new
            {
                ID = 202,
                IP = "192.168.1.100",
                CabAActive = true,
                CabBActive = false,
                CabineAKeyStatus = true,
                CabineBKeyStatus = false,
                TrainCoupledOrder = 2,
                IsTrainCoupled = true,
                AllDoorOpen = false,
                AllDoorClose = true,
                AllDoorReleased = false,
                AllLeftDoorOpen = false,
                AllRightDoorOpen = true,
                AllLeftDoorClose = true,
                AllRightDoorClose = false,
                AllLeftDoorReleased = false,
                AllRightDoorReleased = true
            };

            var data = new
            {
                TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                MasterTrainId = train.ID,
                TrainSpeed = 45.6,
                ZeroSpeed = false,
                TachoMeterPulse = tachoMeterPulse,  // Pulse durumunu burada kullanıyoruz
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Time = DateTime.UtcNow.ToString("HH:mm:ss"),
                CouplingTrainsId = new
                {
                    CouplingTrainsIdXX1 = 201,
                    CouplingTrainsIdXX2 = 202,
                    CouplingTrainsIdXX3 = 203,
                    CouplingTrainsIdXXX = 204
                },
                Train = train
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            string jsonData = JsonSerializer.Serialize(data, options);
            byte[] sendBytes = Encoding.UTF8.GetBytes(jsonData);
            await _udpClient.SendAsync(sendBytes, sendBytes.Length, _targetIp, _targetPort);

            Console.WriteLine($"Veri gönderildi: {jsonData}");
            await Task.Delay(500, stoppingToken);
        }
    }

    // W tuşuna basıldığında pulse'u true yapıyoruz, tuş bırakıldığında ise pulse'u false yapıyoruz
    private void MonitorKeyPress()
    {
        while (true)
        {
            // Eğer bir tuş basıldıysa
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);  // Tuş basımını dinliyoruz

                if (key.Key == ConsoleKey.W)
                {
                    if (!_isWKeyPressed)
                    {
                        // Tuş ilk kez basıldığında, pulse aktif edilir
                        _isPulseActive = true;
                        _isWKeyPressed = true;
                        Console.WriteLine("Pulse aktif edildi.");
                    }
                }
            }
            else
            {
                if (_isWKeyPressed)
                {
                    // W tuşu bırakıldığında, pulse pasif yapılır
                    _isPulseActive = false;
                    _isWKeyPressed = false;
                    Console.WriteLine("Pulse pasif edildi.");
                }
            }

            // CPU'yu boşa harcamamamız için bir süre bekle
            Thread.Sleep(500);
        }
    }

    // Dispose metodu, UDP istemcisini kapatmak için kullanılır
    public override void Dispose()
    {
        _udpClient?.Close();
        base.Dispose();
    }

    // Pulse aktif olup olmadığını döndüren bir metod
    public bool IsPulseActive()
    {
        return _isPulseActive;
    }
}

