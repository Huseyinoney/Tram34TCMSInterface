using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

public class UdpSenderBackgroundService : BackgroundService
{
    private string _targetIp; // Hedef cihazın IP adresi
    private int _targetPort;// Hedef port
    private readonly UdpClient _udpClient;
    private bool _isPulseActive = false;  // Pulse durumunu takip etmek için bir değişken
    private bool _isWKeyPressed = false;  // W tuşunun basılı olup olmadığını kontrol etmek için bir değişken
    private double _trainSpeed = 0;  // Trenin hızını takip eden değişken
    private bool _zeroSpeed = true;  // Trenin durduğunu belirten değişken
    private bool _doorsOpen = false;
    private readonly IConfiguration configuration;

    public UdpSenderBackgroundService(IConfiguration configuration)
    {
        _udpClient = new UdpClient(5005);
        this.configuration = configuration;

        _targetIp = configuration["UDP:Address"] ?? "127.0.0.1"; // Varsayılan değer
        _targetPort = int.TryParse(configuration["UDP:Port"], out int port) ? port : 6000; // Varsayılan port
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
                ID = configuration["TrainSettings:ID"],
                IP = configuration["TrainSettings:IP"],
                Cab_A_Active = Convert.ToBoolean(configuration["TrainSettings:Cab_A_Active"]),
                Cab_B_Active = Convert.ToBoolean(configuration["TrainSettings:Cab_B_Active"]),
                Cab_A_KeyStatus = Convert.ToBoolean(configuration["TrainSettings:Cab_A_KeyStatus"]),
                Cab_B_KeyStatus = Convert.ToBoolean(configuration["TrainSettings:Cab_B_KeyStatus"]),
                TrainCoupledOrder =Convert.ToInt32(configuration["TrainSettings:TrainCoupledOrder"]),
                IsTrainCoupled = Convert.ToBoolean(configuration["TrainSettings:IsTrainCoupled"]),
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
                MasterTrainId = configuration["TrainSettings:MasterTrainId"],
                TrainSpeed = _trainSpeed,
                ZeroSpeed = _zeroSpeed,
                TachoMeterPulse = tachoMeterPulse,  // Pulse durumunu burada kullanıyoruz
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Time = DateTime.UtcNow.ToString("HH:mm:ss"),
                CouplingTrainsId = new
                {
                    CouplingTrainsIdXX1 = configuration["TrainSettings:CouplingTrainsIdXX1"],
                    CouplingTrainsIdXX2 = configuration["TrainSettings:CouplingTrainsIdXX2"],
                    CouplingTrainsIdXX3 = configuration["TrainSettings:CouplingTrainsIdXX3"],
                    CouplingTrainsIdXX4 = configuration["TrainSettings:CouplingTrainsIdXX4"]
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

            //Console.WriteLine($"Veri gönderildi: {jsonData}\n");
            await Task.Delay(int.Parse(configuration["TrainSettings:delay"]), stoppingToken);
        }
    }

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