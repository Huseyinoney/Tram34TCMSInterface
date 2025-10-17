//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Net.Sockets;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Text.Json;

//public class UdpSenderBackgroundService : BackgroundService
//{
//    private string _targetIp;
//    private int _targetPort;
//    private readonly UdpClient _udpClient;
//    private readonly IConfiguration configuration;

//    private bool _isPulseActive = false;
//    private bool _isWKeyPressed = false;
//    private double _trainSpeed = 0;
//    private bool _zeroSpeed = true;
//    private bool _doorsOpen = false;

//    private readonly object _lock = new(); // Paylaşılan verilere thread-safe erişim için

//    public UdpSenderBackgroundService(IConfiguration configuration)
//    {
//        _udpClient = new UdpClient(5005);
//        this.configuration = configuration;

//        _targetIp = configuration["UDP:Address"] ?? "127.0.0.1";
//        _targetPort = int.TryParse(configuration["UDP:Port"], out int port) ? port : 6000;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        Task.Run(() => MonitorKeyPress());

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            bool tachoMeterPulse, zeroSpeed, doorsOpen;
//            double trainSpeed;

//            lock (_lock)
//            {
//                tachoMeterPulse = _isPulseActive;
//                zeroSpeed = _zeroSpeed;
//                trainSpeed = _trainSpeed;
//                doorsOpen = _doorsOpen;
//            }

//            var train = new
//            {
//                ID = configuration["TrainSettings:ID"],
//                IP = configuration["TrainSettings:IP"],
//                Cab_A_Active = Convert.ToBoolean(configuration["TrainSettings:Cab_A_Active"]),
//                Cab_B_Active = Convert.ToBoolean(configuration["TrainSettings:Cab_B_Active"]),
//                Cab_A_KeyStatus = Convert.ToBoolean(configuration["TrainSettings:Cab_A_KeyStatus"]),
//                Cab_B_KeyStatus = Convert.ToBoolean(configuration["TrainSettings:Cab_B_KeyStatus"]),
//                TrainCoupledOrder = Convert.ToInt32(configuration["TrainSettings:TrainCoupledOrder"]),
//                IsTrainCoupled = Convert.ToBoolean(configuration["TrainSettings:IsTrainCoupled"]),
//                AllDoorOpen = doorsOpen,
//                AllDoorClose = !doorsOpen,
//                AllDoorReleased = doorsOpen,
//                AllLeftDoorOpen = doorsOpen,
//                AllRightDoorOpen = doorsOpen,
//                AllLeftDoorClose = !doorsOpen,
//                AllRightDoorClose = !doorsOpen,
//                AllLeftDoorReleased = doorsOpen,
//                AllRightDoorReleased = doorsOpen
//            };

//            var data = new
//            {
//                TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
//                MasterTrainId = configuration["TrainSettings:MasterTrainId"],
//                TrainSpeed = trainSpeed,
//                ZeroSpeed = zeroSpeed,
//                TachoMeterPulse = tachoMeterPulse,
//                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
//                Time = DateTime.UtcNow.ToString("HH:mm:ss"),
//                CouplingTrainsId = new
//                {
//                    CouplingTrainsIdXX1 = configuration["TrainSettings:CouplingTrainsIdXX1"],
//                    CouplingTrainsIdXX2 = configuration["TrainSettings:CouplingTrainsIdXX2"],
//                    CouplingTrainsIdXX3 = configuration["TrainSettings:CouplingTrainsIdXX3"],
//                    CouplingTrainsIdXX4 = configuration["TrainSettings:CouplingTrainsIdXX4"]
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

//            await Task.Delay(int.Parse(configuration["TrainSettings:delay"]));
//        }
//    }

//    private void MonitorKeyPress()
//    {
//        while (true)
//        {
//            if (IsKeyDown(ConsoleKey.W))
//            {
//                if (!_isWKeyPressed)
//                {
//                    lock (_lock)
//                    {
//                        _isPulseActive = true;
//                        _isWKeyPressed = true;
//                        _trainSpeed = 45.6;
//                        _zeroSpeed = false;
//                    }
//                    Console.WriteLine("Pulse aktif edildi, tren hızlandı.");
//                }
//            }
//            else
//            {
//                if (_isWKeyPressed)
//                {
//                    lock (_lock)
//                    {
//                        _isPulseActive = false;
//                        _isWKeyPressed = false;
//                        _trainSpeed = 0;
//                        _zeroSpeed = true;
//                    }
//                    Console.WriteLine("Pulse pasif edildi, tren durdu.");
//                }
//            }

//            if (IsKeyDown(ConsoleKey.D))
//            {
//                lock (_lock)
//                {
//                    _doorsOpen = !_doorsOpen;
//                }
//                Console.WriteLine(_doorsOpen ? "Kapılar açıldı." : "Kapılar kapandı.");
//            }

//            Thread.Sleep(90); // 500dü
//        }
//    }

//    [DllImport("user32.dll")]
//    private static extern short GetAsyncKeyState(int vKey);

//    private static bool IsKeyDown(ConsoleKey key)
//    {
//        return (GetAsyncKeyState((int)key) & 0x8000) != 0;
//    }

//    public override void Dispose()
//    {
//        _udpClient?.Close();
//        base.Dispose();
//    }

//    public bool IsPulseActive()
//    {
//        lock (_lock)
//        {
//            return _isPulseActive;
//        }
//    }
//}



using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

public class UdpSenderBackgroundService : BackgroundService
{
    private string _targetIp;
    private int _targetPort;
    private readonly UdpClient _udpClient;
    private readonly IConfiguration configuration;

    private bool _isPulseActive = false;
    private bool _isWKeyPressed = false;
    private double _trainSpeed = 0;
    private bool _zeroSpeed = true;
    private bool _doorsOpen = false;

    private readonly object _lock = new(); // Paylaşılan verilere thread-safe erişim için
    private readonly List<string> _coupledTrains = new(); // Kuplajlı trenleri tutar

    public UdpSenderBackgroundService(IConfiguration configuration)
    {
        _udpClient = new UdpClient(5005);
        this.configuration = configuration;

        _targetIp = configuration["UDP:Address"] ?? "127.0.0.1";
        _targetPort = int.TryParse(configuration["UDP:Port"], out int port) ? port : 6000;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Task.Run(() => MonitorKeyPress());

        while (!stoppingToken.IsCancellationRequested)
        {
            bool tachoMeterPulse, zeroSpeed, doorsOpen;
            double trainSpeed;
            string[] coupledTrains;

            lock (_lock)
            {
                tachoMeterPulse = _isPulseActive;
                zeroSpeed = _zeroSpeed;
                trainSpeed = _trainSpeed;
                doorsOpen = _doorsOpen;
                coupledTrains = _coupledTrains.ToArray();
            }

            var train = new
            {
                ID = configuration["TrainSettings:ID"],
                IP = configuration["TrainSettings:IP"],
                Cab_A_Active = Convert.ToBoolean(configuration["TrainSettings:Cab_A_Active"]),
                Cab_B_Active = Convert.ToBoolean(configuration["TrainSettings:Cab_B_Active"]),
                Cab_A_KeyStatus = Convert.ToBoolean(configuration["TrainSettings:Cab_A_KeyStatus"]),
                Cab_B_KeyStatus = Convert.ToBoolean(configuration["TrainSettings:Cab_B_KeyStatus"]),
                TrainCoupledOrder = Convert.ToInt32(configuration["TrainSettings:TrainCoupledOrder"]),
                IsTrainCoupled = coupledTrains.Length > 0,
                AllDoorOpen = doorsOpen,
                AllDoorClose = !doorsOpen,
                AllDoorReleased = doorsOpen,
                AllLeftDoorOpen = doorsOpen,
                AllRightDoorOpen = doorsOpen,
                AllLeftDoorClose = !doorsOpen,
                AllRightDoorClose = !doorsOpen,
                AllLeftDoorReleased = doorsOpen,
                AllRightDoorReleased = doorsOpen
            };

            var data = new
            {
                TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                MasterTrainId = configuration["TrainSettings:MasterTrainId"],
                TrainSpeed = trainSpeed,
                ZeroSpeed = zeroSpeed,
                TachoMeterPulse = tachoMeterPulse,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Time = DateTime.UtcNow.ToString("HH:mm:ss"),
                CouplingTrainsId = new
                {
                    CouplingTrainsIdXX1 = coupledTrains.ElementAtOrDefault(0) ?? "",
                    CouplingTrainsIdXX2 = coupledTrains.ElementAtOrDefault(1) ?? "",
                    CouplingTrainsIdXX3 = coupledTrains.ElementAtOrDefault(2) ?? "",
                    CouplingTrainsIdXX4 = coupledTrains.ElementAtOrDefault(3) ?? ""
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

            await Task.Delay(int.Parse(configuration["TrainSettings:delay"]));
        }
    }

    private async void MonitorKeyPress()
    {
        while (true)
        {
            // Hız kontrolü (W)
            if (IsKeyDown(ConsoleKey.W))
            {
                if (!_isWKeyPressed)
                {
                    lock (_lock)
                    {
                        _isPulseActive = true;
                        _isWKeyPressed = true;
                        _trainSpeed = 45.6;
                        _zeroSpeed = false;
                    }
                    Console.WriteLine("Pulse aktif edildi, tren hızlandı.");
                }
            }
            else
            {
                if (_isWKeyPressed)
                {
                    lock (_lock)
                    {
                        _isPulseActive = false;
                        _isWKeyPressed = false;
                        _trainSpeed = 0;
                        _zeroSpeed = true;
                    }
                    Console.WriteLine("Pulse pasif edildi, tren durdu.");
                }
            }

            // Kapı kontrolü (D)
            if (IsKeyDown(ConsoleKey.D))
            {
                lock (_lock)
                {
                    _doorsOpen = !_doorsOpen;
                }
                Console.WriteLine(_doorsOpen ? "Kapılar açıldı." : "Kapılar kapandı.");
                // Thread.Sleep(200);
                await Task.Delay(200);
            }

            // Kuplaj toggle (1-4)
            if (IsKeyDown(ConsoleKey.D1))
                ToggleCoupling("Train 1");
            if (IsKeyDown(ConsoleKey.D2))
                ToggleCoupling("Train 2");
            if (IsKeyDown(ConsoleKey.D3))
                ToggleCoupling("Train 16");
            if (IsKeyDown(ConsoleKey.D4))
                ToggleCoupling("Train 3");

            //Thread.Sleep(90);
            await Task.Delay(90);
        }
    }

    private void ToggleCoupling(string trainId)
    {
        lock (_lock)
        {
            if (_coupledTrains.Contains(trainId))
            {
                _coupledTrains.Remove(trainId);
                Console.WriteLine($"{trainId} kuplajdan çıkarıldı.");
            }
            else
            {
                if (_coupledTrains.Count < 4) // Maksimum 4 tren
                {
                    _coupledTrains.Add(trainId);
                    Console.WriteLine($"{trainId} kuplajlandı.");
                }
                else
                {
                    Console.WriteLine("Maksimum 4 tren kuplajlı olabilir.");
                }
            }
        }
        Thread.Sleep(200); // debounce
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool IsKeyDown(ConsoleKey key)
    {
        return (GetAsyncKeyState((int)key) & 0x8000) != 0;
    }

    public override void Dispose()
    {
        _udpClient?.Close();
        base.Dispose();
    }

    public bool IsPulseActive()
    {
        lock (_lock)
        {
            return _isPulseActive;
        }
    }
}


