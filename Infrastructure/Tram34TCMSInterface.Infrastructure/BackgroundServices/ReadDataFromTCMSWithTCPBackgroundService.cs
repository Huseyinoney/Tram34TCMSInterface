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
                        Console.WriteLine("Döndü");
                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        socket.Bind(new IPEndPoint(localIPAddress, localPort));

                        _client = new TcpClient { Client = socket};
                        Console.WriteLine("ConnectAsync a gidiyor");
                        await _client.ConnectAsync(remoteIPAddress, remotePort, stoppingToken);
                        Console.WriteLine("ConnectAsyncdan çıktı");
                        _stream = _client.GetStream();
                        isConnected = true;

                        Console.WriteLine($"\nBağlandı: {localIPAddress}:{localPort} → {remoteIPAddress}:{remotePort}\n");
                        logService.SendLogAsync<EventLog>(logFactory.CreateEventLog($"\nBağlandı: {localIPAddress}:{localPort} → {remoteIPAddress}:{remotePort}\n","TCMSInterface",GetLocalIPAddress(),"Socket Connection Status",GetLocalIPAddress()));

                        // Yazma task’i başlat
                        _ = Task.Run(() => HandleWriteAsync(_stream, stoppingToken), stoppingToken);
                    }

                    await HandleServerAsync(_client, _stream, stoppingToken);
                    await Task.Delay(int.Parse(configuration["TCP:delayToRabbit"]));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nBağlantı hatası: {ex.Message}\n");
                    logService.SendLogAsync<ErrorLog>(logFactory.CreateErrorLog($"\nBağlantı hatası: {ex.Message}\n","TCMSInterface","Software",GetLocalIPAddress()));
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
            try { _stream?.Close(); } catch { }
            try { _stream?.Dispose(); } catch { }

            try
            {
                if (_client?.Client?.Connected == true)
                {
                    _client.Client.Shutdown(SocketShutdown.Both);
                }
            }
            catch { }

            try { _client?.Close(); } catch { }
            try { _client?.Dispose(); } catch { }

            _stream = null;
            _client = null;
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
                logService.SendLogAsync<ErrorLog>(logFactory.CreateErrorLog($"Discard sırasında hata oluştu: {ex.Message}", "TCMSInterface", "Software", GetLocalIPAddress()));
            }
        }



        //private async Task DiscardUntilNewlineAsync(NetworkStream stream, List<byte> buffer, CancellationToken token)
        //{
        //    var discardBuffer = new byte[1];
        //    const int maxBytesToDiscard = 2048;
        //    int bytesDiscarded = 0;

        //    while (!token.IsCancellationRequested && bytesDiscarded < maxBytesToDiscard)
        //    {
        //        if (buffer.Count > 0)
        //        {
        //            if (buffer[0] == (byte)'\n')
        //            {
        //                buffer.RemoveAt(0);
        //                Console.WriteLine($"\nDiscard: \\n bulundu. {bytesDiscarded} byte atlandı.\n");
        //                return;
        //            }
        //            buffer.RemoveAt(0);
        //            bytesDiscarded++;
        //            continue;
        //        }

        //        int read = await stream.ReadAsync(discardBuffer, 0, 1, token);
        //        if (read == 0 || discardBuffer[0] == (byte)'\n')
        //        {
        //            Console.WriteLine($"\nDiscard: \\n bulundu. {bytesDiscarded} byte atlandı.\n");
        //            return;
        //        }
        //        bytesDiscarded++;
        //    }

        //    if (bytesDiscarded >= maxBytesToDiscard)
        //        Console.WriteLine("\nUyarı: \\n bulunamadı, buffer temizleme sınırı aşıldı.\n");
        //}

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

                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\nOkuma hatası: {ex.Message}\n");
                        logService.SendLogAsync<ErrorLog>(logFactory.CreateErrorLog($"\nOkuma hatası: {ex.Message}\n", "TCMSInterface", "Software", GetLocalIPAddress()));

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
                            logService.SendLogAsync<ErrorLog>(logFactory.CreateErrorLog($"\nUyarı: JSON deserialize hatası: {ex.Message}\n", "TCMSInterface", "Software", GetLocalIPAddress()));
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
                logService.SendLogAsync<ErrorLog>(logFactory.CreateErrorLog($"\nVeri işleme hatası: {ex.Message} \n", "TCMSInterface", "Software", GetLocalIPAddress()));
                //CleanupConnection();
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


        //private bool HasNullProperty(object obj)
        //{
        //    if (obj == null) return true;
        //    var type = obj.GetType();
        //    foreach (var property in type.GetProperties())
        //    {
        //        if (property.Name == nameof(JsonDocumentFormatUDP.TrainData.CouplingTrainsId)) continue;
        //        var value = property.GetValue(obj);
        //        if (value == null) return true;
        //        if (!property.PropertyType.IsPrimitive && property.PropertyType != typeof(string))
        //            if (HasNullProperty(value)) return true;
        //    }
        //    return false;
        //}


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


        //private string GetLocalIPAddress()
        //{
        //    var host = Dns.GetHostEntry(Dns.GetHostName());
        //    foreach (var ip in host.AddressList)
        //        if (ip.AddressFamily == AddressFamily.InterNetwork)
        //            return ip.ToString();
        //    return "127.0.0.1";
        //}

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
                    Heartbeat =Heartbeat++,
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
                    Console.WriteLine("\nGönderildi: " + json+"\n");

                    logService.SendLogAsync<EventLog>(logFactory.CreateEventLog("TCMS'e Veri Paketi Gönderildi.", "TCMSInterface", GetLocalIPAddress(), "Socket", GetLocalIPAddress()));

                }
                catch(ObjectDisposedException ex)
                {
                    CleanupConnection();
                    throw new Exception(ex.Message);
                }
                catch (Exception ex)
                {

                    Console.WriteLine("\nGönderim hatası: " + ex.Message+"\n");
                    logService.SendLogAsync<ErrorLog>(logFactory.CreateErrorLog("\nGönderim hatası: " + ex.Message + "\n", "TCMSInterface", "Software", GetLocalIPAddress()));
                    //break;
                    throw new Exception(ex.Message);
                }

                await Task.Delay(500, cancellationToken);
            }
        }
    }
}