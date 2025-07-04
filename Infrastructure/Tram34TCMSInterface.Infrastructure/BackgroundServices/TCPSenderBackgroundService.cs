using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
{
    public class TCPSenderBackgroundService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private bool _isPulseActive = false;
        private bool _isWKeyPressed = false;
        private double _trainSpeed = 0;
        private bool _zeroSpeed = true;
        private bool _doorsOpen = false;

        public TCPSenderBackgroundService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string ip = _configuration["TCP:TargetIP"]; //bağlanılacak server ıp
            int port = int.Parse(_configuration["TCP:TargetPort"]); // bağlanılacak server ın portu
            int delayMs = int.Parse(_configuration["TrainSettings:delay"]);

            await ConnectAsync(ip, port, stoppingToken);

            _ = Task.Run(() => MonitorKeyPress(), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_stream == null || !_tcpClient.Connected)
                {
                    Console.WriteLine("Bağlantı koptu, tekrar bağlanmayı deniyor...");
                    await Task.Delay(2000, stoppingToken);
                    await ConnectAsync(ip, port, stoppingToken);
                    continue;
                }

                var train = new
                {
                    ID = _configuration["TrainSettings:ID"],
                    IP = _configuration["TrainSettings:IP"],
                    Cab_A_Active = Convert.ToBoolean(_configuration["TrainSettings:Cab_A_Active"]),
                    Cab_B_Active = Convert.ToBoolean(_configuration["TrainSettings:Cab_B_Active"]),
                    Cab_A_KeyStatus = Convert.ToBoolean(_configuration["TrainSettings:Cab_A_KeyStatus"]),
                    Cab_B_KeyStatus = Convert.ToBoolean(_configuration["TrainSettings:Cab_B_KeyStatus"]),
                    TrainCoupledOrder = Convert.ToInt32(_configuration["TrainSettings:TrainCoupledOrder"]),
                    IsTrainCoupled = Convert.ToBoolean(_configuration["TrainSettings:IsTrainCoupled"]),
                    AllDoorOpen = _doorsOpen,
                    AllDoorClose = !_doorsOpen,
                    AllDoorReleased = _doorsOpen,
                    AllLeftDoorOpen = _doorsOpen,
                    AllRightDoorOpen = _doorsOpen,
                    AllLeftDoorClose = !_doorsOpen,
                    AllRightDoorClose = !_doorsOpen,
                    AllLeftDoorReleased = _doorsOpen,
                    AllRightDoorReleased = _doorsOpen,
                    YBS_Intercom_1 = false,
                    YBS_Intercom_2 = false,
                    YBS_Intercom_3 = false,
                    YBS_Intercom_4 = false,
                    YBS_Intercom_5 = false,
                    YBS_Intercom_6 = false,
                    YBS_Intercom_7 = false,
                    YBS_Intercom_8 = false,
                };

                var emergencyState = new
                {
                    EmergencyStopHandle1 = false,
                    EmergencyStopHandle2 = false,
                    EmergencyStopHandle3 = false,
                    EmergencyStopHandle4 = false,
                    EmergencyStopHandle5 = false,
                    EmergencyStopHandle6 = false,
                    EmergencyStopHandle7 = false,
                    EmergencyStopHandle8 = false,
                    FireDetectState1 = false,
                    FireDetectState2 = false,
                };

                var data = new
                {
                    TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                    MasterTrainId = _configuration["TrainSettings:MasterTrainId"],
                    TrainSpeed = _trainSpeed,
                    ZeroSpeed = _zeroSpeed,
                    TachoMeterPulse = _isPulseActive,
                    Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    Time = DateTime.UtcNow.ToString("HH:mm:ss"),
                    CouplingTrainsId = new
                    {
                        CouplingTrainsIdXX1 = _configuration["TrainSettings:CouplingTrainsIdXX1"],
                        CouplingTrainsIdXX2 = _configuration["TrainSettings:CouplingTrainsIdXX2"],
                        CouplingTrainsIdXX3 = _configuration["TrainSettings:CouplingTrainsIdXX3"],
                        CouplingTrainsIdXX4 = _configuration["TrainSettings:CouplingTrainsIdXX4"]
                    },
                    TRAIN = train,
                    EMERGENCYSTATE = emergencyState,
                };

                string jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    // PropertyNamingPolicy = JsonNamingPolicy.CamelCase  
                });

              

                  byte[] bytes = Encoding.UTF8.GetBytes(jsonData + "\n"); // "\n" delimiter is optional
                //byte[] bytes = Encoding.UTF8.GetBytes(jsonData); // "\n" delimiter is optional
                try
                {
                    await _stream.WriteAsync(bytes, 0, bytes.Length, stoppingToken);
                    //Console.WriteLine("Veri gönderildi");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Veri gönderme hatası: {ex.Message}");
                    _tcpClient.Close();
                }

                await Task.Delay(delayMs, stoppingToken);
            }
        }

        //private async Task ConnectAsync(string ip, int port, CancellationToken token)
        //{
        //    try
        //    {
        //        _tcpClient?.Close();
        //        _tcpClient = new TcpClient();

        //        await _tcpClient.ConnectAsync(ip, port);
        //        _stream = _tcpClient.GetStream();
        //        Console.WriteLine("TCP bağlantısı kuruldu.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Bağlantı hatası: {ex.Message}");
        //    }
        //}
        private async Task ConnectAsync(string ip, int port, CancellationToken token)
        {
            try
            {
                _tcpClient?.Close();

                int localPort = int.Parse(_configuration["TCP:LocalPort"]); // appsettings.json'dan al

                // Özel bir Socket oluştur
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Gerekirse ReuseAddress ayarı
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Belirli bir yerel porta bind et
                socket.Bind(new IPEndPoint(IPAddress.Any, localPort));

                // TcpClient içine bu socket'i yerleştir
                _tcpClient = new TcpClient { Client = socket };

                await _tcpClient.ConnectAsync(IPAddress.Parse(ip), port);
                _stream = _tcpClient.GetStream();
                Console.WriteLine($"TCP bağlantısı kuruldu. Yerel Port: {localPort}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Bağlantı hatası: {ex.Message}");
            }
        }


        private void MonitorKeyPress()
        {
            while (true)
            {
                if (IsKeyDown(ConsoleKey.W))
                {
                    if (!_isWKeyPressed)
                    {
                        _isPulseActive = true;
                        _isWKeyPressed = true;
                        _trainSpeed = 45.6;
                        _zeroSpeed = false;
                        Console.WriteLine("Tren hareket ediyor.");
                    }
                }
                else
                {
                    if (_isWKeyPressed)
                    {
                        _isPulseActive = false;
                        _isWKeyPressed = false;
                        _trainSpeed = 0;
                        _zeroSpeed = true;
                        Console.WriteLine("Tren durdu.");
                    }
                }

                if (IsKeyDown(ConsoleKey.D))
                {
                    _doorsOpen = !_doorsOpen;
                    Console.WriteLine(_doorsOpen ? "Kapılar açıldı." : "Kapılar kapandı.");
                }

                Thread.Sleep(500);
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static bool IsKeyDown(ConsoleKey key)
        {
            return (GetAsyncKeyState((int)key) & 0x8000) != 0;
        }

        public override void Dispose()
        {
            _tcpClient?.Close();
            base.Dispose();
        }

    }
}
