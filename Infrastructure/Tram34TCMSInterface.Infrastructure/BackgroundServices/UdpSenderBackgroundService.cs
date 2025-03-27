using Microsoft.Extensions.Hosting;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

public class UdpSenderBackgroundService : BackgroundService
{
    private readonly string _targetIp = "100.10.133.33"; // Hedef cihazın IP adresi
    private readonly int _targetPort = 6000; // Hedef port
    private readonly UdpClient _udpClient;
    private bool _isPulseActive = false;  // Pulse durumunu takip etmek için bir değişken
    private bool _isWKeyPressed = false;  // W tuşunun basılı olup olmadığını kontrol etmek için bir değişken
    private double _trainSpeed = 0;  // Trenin hızını takip eden değişken
    private bool _zeroSpeed = true;  // Trenin durduğunu belirten değişken
    private bool _doorsOpen = false;

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
                ID = 7,
                IP = "100.10.107.20",
                CabAActive = true,
                CabBActive = false,
                CabineAKeyStatus = true,
                CabineBKeyStatus = false,
                TrainCoupledOrder = 2,
                IsTrainCoupled = true,
                AllDoorOpen = _doorsOpen,
                AllDoorClose = !_doorsOpen,
                AllDoorReleased = _doorsOpen,
                AllLeftDoorOpen = _doorsOpen,
                AllRightDoorOpen = _doorsOpen,
                AllLeftDoorClose = !_doorsOpen,
                AllRightDoorClose = !_doorsOpen,
                AllLeftDoorReleased = _doorsOpen,
                AllRightDoorReleased = _doorsOpen
            };

            var data = new
            {
                TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                MasterTrainId = train.ID,
                TrainSpeed = _trainSpeed,
                ZeroSpeed = _zeroSpeed,
                TachoMeterPulse = tachoMeterPulse,  // Pulse durumunu burada kullanıyoruz
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Time = DateTime.UtcNow.ToString("HH:mm:ss"),
                CouplingTrainsId = new
                {
                    CouplingTrainsIdXX1 = 2,
                    CouplingTrainsIdXX2 = 7,
                    CouplingTrainsIdXX3 = 16,
                    CouplingTrainsIdXXX = 28
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
    //private void MonitorKeyPress()
    //{
    //    while (true)
    //    {
    //        if (IsKeyDown(ConsoleKey.W))
    //        {
    //            if (!_isWKeyPressed)
    //            {
    //                _isPulseActive = true;
    //                _isWKeyPressed = true;
    //                _trainSpeed = 45.6;
    //                _zeroSpeed = false;
    //                Console.WriteLine("Pulse aktif edildi.");
    //            }
    //        }
    //        else  // W tuşu bırakıldıysa
    //        {
    //            if (_isWKeyPressed)
    //            {
    //                _isPulseActive = false;
    //                _isWKeyPressed = false;
    //                _trainSpeed = 0;
    //                _zeroSpeed = true;
    //                Console.WriteLine("Pulse pasif edildi.");
    //            }
    //        }

    //        Thread.Sleep(500);  // Daha hızlı tepki vermesi için bekleme süresini azalttık
    //    }
    //}

    private void MonitorKeyPress()
    {
        while (true)
        {
            // W tuşuna basılıysa
            if (IsKeyDown(ConsoleKey.W))
            {
                if (!_isWKeyPressed)
                {
                    _isPulseActive = true;
                    _isWKeyPressed = true;
                    _trainSpeed = 45.6;
                    _zeroSpeed = false;
                    Console.WriteLine("Pulse aktif edildi, tren hızlandı.");
                }
            }
            else  // W tuşu bırakıldığında
            {
                if (_isWKeyPressed)
                {
                    _isPulseActive = false;
                    _isWKeyPressed = false;
                    _trainSpeed = 0;
                    _zeroSpeed = true;
                    Console.WriteLine("Pulse pasif edildi, tren durdu.");
                }
            }

            // D tuşuna basıldığında kapıları aç/kapat
            if (IsKeyDown(ConsoleKey.D))
            {
                _doorsOpen = !_doorsOpen; // Kapıları toggle (aç/kapat)
                Console.WriteLine(_doorsOpen ? "Kapılar açıldı." : "Kapılar kapandı.");
                // Tuşun tekrar basılmasını önlemek için cooldown ekliyoruz
            }

            Thread.Sleep(500);  // Daha akıcı yanıt verebilmesi için bekleme süresini düşürdük
        }
    }

    // Windows API ile tuşun basılı olup olmadığını kontrol eden fonksiyon
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool IsKeyDown(ConsoleKey key)
    {
        return (GetAsyncKeyState((int)key) & 0x8000) != 0;
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