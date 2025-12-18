using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tram34TCMSInterface.Application.Features.ReadDataFromTCMSWithTCP;
using Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS;
using Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS;
using Tram34TCMSInterface.Domain.Models;
using Tram34TCMSInterface.Application.Abstractions.LogService;
using Tram34TCMSInterface.Domain.Log;

namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
{
    public class ReadDataFromTCMSWithTCPBackgroundService : BackgroundService
    {
        private readonly IMediator mediator;
        private readonly IConfiguration configuration;
        private TcpClient _client;
        private NetworkStream _stream;
        private bool isConnected = false;
        private readonly ILogFactory logFactory;
        private readonly ILogService logService;

        public ReadDataFromTCMSWithTCPBackgroundService(IMediator mediator, IConfiguration configuration, ILogFactory logFactory, ILogService logService)
        {
            this.mediator = mediator;
            this.configuration = configuration;
            this.logFactory = logFactory;
            this.logService = logService;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string? ip = configuration["TCP:Address"];
            string? portStr = configuration["TCP:SourcePort"];
            string? localIpStr = configuration["TCP:LocalIP"];
            string? localPortStr = configuration["TCP:LocalPort"];

            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(localIpStr))
                throw new ArgumentException("TCP IP adresleri boş olamaz.");

            if (!int.TryParse(portStr, out int remotePort) || !int.TryParse(localPortStr, out int localPort))
                throw new ArgumentException("TCP port bilgileri hatalı.");

            if (!IPAddress.TryParse(ip, out var remoteIPAddress) || !IPAddress.TryParse(localIpStr, out var localIPAddress))
                throw new ArgumentException("IP adres formatı hatalı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!isConnected || _client == null || !_client.Connected || !IsSocketConnected(_client.Client))
                    {

                        CleanupConnection();

                        _client = new TcpClient(AddressFamily.InterNetwork);
                        _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        //_client.Client.Bind(new IPEndPoint(localIPAddress, localPort));

                        using var cts = new CancellationTokenSource(int.Parse(configuration["TCP:Wait"]));
                        await _client.ConnectAsync(remoteIPAddress, remotePort, cts.Token);

                        isConnected = true;
                        _stream = _client.GetStream();

                        Console.WriteLine($"\nBağlandı: {localIPAddress}:{localPort} → {remoteIPAddress}:{remotePort}\n");

                        // Yazma ve okuma task’lerini sadece bağlantı kurulduğunda başlat
                        CancellationTokenSource connCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        var token = connCts.Token;

                        var readTask = Task.Run(() => HandleServerAsync(_client, _stream, token), token);
                        var writeTask = Task.Run(async () =>
                        {
                            try
                            {
                                await HandleWriteAsync(_stream, token);
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"WriteLoop hatası: {ex.Message}");
                            }
                        }, token);

                        // Read veya write task’lerinden biri tamamlanırsa bağlantıyı kapat
                        var completed = await Task.WhenAny(readTask, writeTask);

                        // Task hata verdiyse logla
                        if (completed.IsFaulted && completed.Exception != null)
                        {
                            Console.WriteLine($"Bağlantı task hatası: {completed.Exception.GetBaseException().Message}");
                        }

                        // Her iki task’i de sonlandır
                        connCts.Cancel();
                        try { await Task.WhenAll(readTask.ContinueWith(_ => { }), writeTask.ContinueWith(_ => { })); } catch { }

                        CleanupConnection();
                    }

                    await Task.Delay(int.Parse(configuration["TCP:delayToRabbit"]), stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nBağlantı hatası: {ex.Message}\n");
                    CleanupConnection();
                    await Task.Delay(500, stoppingToken);
                }
            }
        }

        private bool IsSocketConnected(Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch
            {
                return false;
            }
        }


        private void CleanupConnection()
        {
            try { _stream?.Close(); _stream?.Dispose(); _stream = null; } catch { }

            try
            {
                if (_client?.Connected == true)
                {
                    // TIME_WAIT önlemek için RST gönder
                    _client.Client.LingerState = new LingerOption(true, 0);
                    _client.Client.Shutdown(SocketShutdown.Both);
                    _client.Client.Close();
                }
            }
            catch { }

            try { _client?.Close(); _client?.Dispose(); _client = null; } catch { }

            isConnected = false;
        }

        private async Task DiscardUntilNewlineAsync(NetworkStream stream, List<byte> buffer, CancellationToken token)
        {
            var discardBuffer = new byte[1];
            const int maxBytesToDiscard = 2048;
            int bytesDiscarded = 0;

            try
            {
                while (!token.IsCancellationRequested && bytesDiscarded < maxBytesToDiscard)
                {
                    if (buffer.Count > 0)
                    {
                        if (buffer[0] == (byte)'\n')
                        {
                            buffer.RemoveAt(0);
                            Console.WriteLine($"\nDiscard: \\n bulundu. {bytesDiscarded} byte atlandı.\n");
                            return;
                        }
                        buffer.RemoveAt(0);
                        bytesDiscarded++;
                        continue;
                    }

                    int read = await stream.ReadAsync(discardBuffer, 0, 1, token);
                    if (read == 0 || discardBuffer[0] == (byte)'\n')
                    {
                        Console.WriteLine($"\nDiscard: \\n bulundu. {bytesDiscarded} byte atlandı.\n");
                        return;
                    }
                    bytesDiscarded++;
                }

                if (bytesDiscarded >= maxBytesToDiscard)
                    Console.WriteLine("\nUyarı: \\n bulunamadı, buffer temizleme sınırı aşıldı.\n");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Discard işlemi iptal edildi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Discard sırasında hata oluştu: {ex.Message}");
                if (Convert.ToBoolean(configuration["LogStatus:Error"]))
                {

                    logService.SendLogAsync<ErrorLog>(logFactory.CreateErrorLog($"Discard sırasında hata oluştu: {ex.Message}", configuration["Log:TCMSSource"], "Software", GetLocalIPAddress()));
                }
            }
        }

        private async Task HandleServerAsync(TcpClient client, NetworkStream stream, CancellationToken stoppingToken)
        {
            var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            const int LENGTH_HEADER_SIZE = 4;
            var buffer = new List<byte>();
            var readBuffer = new byte[1250];

            try
            {
                while (!stoppingToken.IsCancellationRequested && client.Connected)
                {
                    int bytesRead = 0;
                    try
                    {
                        bytesRead = await stream.ReadAsync(readBuffer.AsMemory(0, readBuffer.Length), stoppingToken);
                        if (bytesRead == 0)
                        {
                            Console.WriteLine("\nBağlantı kapandı.\n");
                            CleanupConnection();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\nOkuma hatası: {ex.Message}\n");
                        if (Convert.ToBoolean(configuration["LogStatus:Error"]))
                        {

                            logService.SendLogAsync<ErrorLog>(logFactory.CreateErrorLog($"\nOkuma hatası: {ex.Message}\n", configuration["Log:TCMSSource"], "Software", GetLocalIPAddress()));
                        }

                        break;
                    }

                    buffer.AddRange(readBuffer.AsSpan(0, bytesRead).ToArray());

                    while (true)
                    {
                        if (buffer.Count < LENGTH_HEADER_SIZE)
                            break;

                        int messageLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer.GetRange(0, LENGTH_HEADER_SIZE).ToArray());

                        if (messageLength <= 0 || messageLength > 1300)
                        {
                            Console.WriteLine($"\nUyarı: Geçersiz mesaj uzunluğu {messageLength}. Buffer temizleniyor.\n");
                            await DiscardUntilNewlineAsync(stream, buffer, stoppingToken);
                            continue;
                        }

                        int totalMessageSize = LENGTH_HEADER_SIZE + messageLength + 1;

                        if (buffer.Count < totalMessageSize)
                            break;

                        if (buffer[LENGTH_HEADER_SIZE + messageLength] != (byte)'\n')
                        {
                            Console.WriteLine("\nUyarı: Mesaj sonunda '\\n' yok veya hatalı. Buffer temizleniyor.\n");

                            if (buffer.Count >= totalMessageSize)
                            {
                                //buffer.RemoveRange(0, totalMessageSize);
                                buffer.Clear();
                            }
                            else
                            {
                                buffer.Clear();
                            }

                            continue;
                        }

                        var messageBytes = buffer.GetRange(LENGTH_HEADER_SIZE, messageLength).ToArray();
                        string jsonString = Encoding.UTF8.GetString(messageBytes);

                        if (string.IsNullOrWhiteSpace(jsonString))
                        {
                            Console.WriteLine("\nUyarı: Boş JSON mesajı atlanıyor.\n");
                            buffer.RemoveRange(0, totalMessageSize);
                            continue;
                        }

                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        JsonDocumentFormatUDP.TrainData? trainData = null;
                        try
                        {
                            trainData = JsonSerializer.Deserialize<JsonDocumentFormatUDP.TrainData>(jsonString, options);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"\nUyarı: JSON deserialize hatası: {ex.Message}\n");
                            if (Convert.ToBoolean(configuration["LogStatus:Error"]))
                            {

                                logService.SendLogAsync<ErrorLog>(logFactory.CreateErrorLog($"\nUyarı: JSON deserialize hatası: {ex.Message}\n", configuration["Log:TCMSSource"], "Software", GetLocalIPAddress()));
                            }
                            buffer.RemoveRange(0, totalMessageSize);
                            continue;
                        }

                        if (trainData == null || HasNullProperty(trainData))
                        {
                            Console.WriteLine("\nUyarı: Null alanlı veya hatalı JSON. Mesaj atlanıyor.\n");
                            buffer.RemoveRange(0, totalMessageSize);
                            continue;
                        }

                        var command = new ReadDataFromTCMSWithTCPCommand
                        {
                            DataBytes = Encoding.UTF8.GetBytes(jsonString),
                            RemoteEndPoint = remoteEndPoint
                        };

                        var result = await mediator.Send(command, stoppingToken);
                        if (result != null)
                        {
                            await mediator.Send(new SendCoupledDataToCoupleExchangeFromTCMSCommand { trainData = result }, stoppingToken);
                            await mediator.Send(new SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand { trainData = result }, stoppingToken);
                        }

                        buffer.RemoveRange(0, totalMessageSize);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nVeri işleme hatası: {ex.Message} \n");
                if (Convert.ToBoolean(configuration["LogStatus:Error"]))
                {

                    logService.SendLogAsync<ErrorLog>(logFactory.CreateErrorLog($"\nVeri işleme hatası: {ex.Message} \n", configuration["Log:TCMSSource"], "Software", GetLocalIPAddress()));
                }
                CleanupConnection();
            }
        }

        private bool HasNullProperty(object obj)
        {
            if (obj == null) return true;

            try
            {
                var type = obj.GetType();
                foreach (var property in type.GetProperties())
                {
                    if (property.Name == nameof(JsonDocumentFormatUDP.TrainData.CouplingTrainsId))
                        continue;

                    object value;
                    try
                    {
                        value = property.GetValue(obj);
                    }
                    catch
                    {
                        // Property okunamıyorsa null sayılabilir
                        return true;
                    }

                    if (value == null) return true;

                    if (!property.PropertyType.IsPrimitive && property.PropertyType != typeof(string))
                    {
                        if (HasNullProperty(value)) return true;
                    }
                }
            }
            catch
            {
                // Genel hata varsa null kabul et
                return true;
            }

            return false;
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
            }
            catch (SocketException ex)
            {
                // Log veya kullanıcıya mesaj
                Console.WriteLine("\nIP alınamadı: " + ex.Message + "\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nGenel hata: " + ex.Message + "\n");
            }

            return "127.0.0.1"; // fallback
        }
        // burası ayrı bir class a alınacak
        private async Task HandleWriteAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            uint Heartbeat = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                var outgoingData = new
                {
                    TimeStamp = now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    vDate = now.ToString("yyyy-MM-dd"),
                    vTime = now.ToString("HH:mm:ss"),
                    IP = GetLocalIPAddress(),
                    Heartbeat = Heartbeat++,
                    YBS_Announcement_State = true,
                    YBS_Intercom_State = true,
                    YBS_Intercom_1 = false,
                    YBS_Intercom_2 = false,
                    YBS_Intercom_3 = false,
                    YBS_Intercom_4 = false,
                    YBS_Intercom_5 = false,
                    YBS_Intercom_6 = false,
                    YBS_Intercom_7 = false,
                    YBS_Intercom_8 = false
                };

                var json = JsonSerializer.Serialize(outgoingData);
                var bytes = Encoding.UTF8.GetBytes(json);
                byte[] lengthBytes = new byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(lengthBytes, bytes.Length);

                // Uzunluk + JSON + \n
                byte[] packet = new byte[4 + bytes.Length + 1];
                Buffer.BlockCopy(lengthBytes, 0, packet, 0, 4);
                Buffer.BlockCopy(bytes, 0, packet, 4, bytes.Length);
                packet[4 + bytes.Length] = (byte)'\n';

                try
                {
                    await stream.WriteAsync(packet, 0, packet.Length, cancellationToken);
                    Console.WriteLine("\nGönderildi: " + json + "\n");

                    if (Convert.ToBoolean(configuration["LogStatus:Event"]))
                    {

                        logService.SendLogAsync<EventLog>(logFactory.CreateEventLog("TCMS'e Veri Paketi Gönderildi.", configuration["Log:TCMSSource"], GetLocalIPAddress(), "Socket", GetLocalIPAddress()));
                    }

                }
                catch (ObjectDisposedException ex)
                {
                    CleanupConnection();
                    throw new Exception(ex.Message);
                }
                catch (Exception ex)
                {

                    Console.WriteLine("\nGönderim hatası: " + ex.Message + "\n");
                    if (Convert.ToBoolean(configuration["LogStatus:Error"]))
                    {

                        logService.SendLogAsync<ErrorLog>(logFactory.CreateErrorLog("\nGönderim hatası: " + ex.Message + "\n", configuration["Log:TCMSSource"], "Software", GetLocalIPAddress()));
                    }

                    CleanupConnection();
                    throw new Exception(ex.Message);
                }

                await Task.Delay(500, cancellationToken);
            }
        }
    }
}