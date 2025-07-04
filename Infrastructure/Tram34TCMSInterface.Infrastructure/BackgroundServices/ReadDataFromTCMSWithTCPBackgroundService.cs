
//TCMS SERVER SİDE KISMI 

//using MediatR;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using Tram34TCMSInterface.Application.Features.ReadDataFromTCMSWithTCP;
//using Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS;
//using Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS;

//namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
//{
//    public class ReadDataFromTCMSWithTCPBackgroundService : BackgroundService
//    {
//        private readonly IMediator mediator;
//        private readonly TcpListener listener;

//        public ReadDataFromTCMSWithTCPBackgroundService(IMediator mediator, IConfiguration configuration)
//        {
//            this.mediator = mediator;
//            string? ip = configuration["TCP:Address"];
//            string? portStr = configuration["TCP:SourcePort"];

//            if (string.IsNullOrWhiteSpace(ip))
//                throw new ArgumentException("TCP IP address cannot be null or empty. Check configuration [TCP:Address]");

//            if (!int.TryParse(portStr, out int port) || port <= 0 || port > 65535)
//                throw new ArgumentException("TCP port is invalid. Check configuration [TCP:SourcePort]");

//            if (!IPAddress.TryParse(ip, out IPAddress? ipAddress))
//                throw new ArgumentException("TCP IP address format is invalid. Check configuration [TCP:Address]");

//            listener = new TcpListener(ipAddress, port);
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            Console.WriteLine("TCP Background Service Başladı...");

//            listener.Start();

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    var client = await listener.AcceptTcpClientAsync(stoppingToken);
//                    _ = Task.Run(() => HandleClientAsync(client, stoppingToken), stoppingToken); // fire and forget
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Bağlantı alma hatası: {ex.Message}");
//                }
//            }
//        }

//        private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
//        {
//            Console.WriteLine($"Yeni bağlantı: {client.Client.RemoteEndPoint}");

//            using var stream = client.GetStream();

//            var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

//            try
//            {
//                while (!stoppingToken.IsCancellationRequested && client.Connected)
//                {
//                    int i = 0;
//                    byte[] buffer = new byte[4096];
//                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);



//                    if (bytesRead == 0)
//                    {
//                        Console.WriteLine("İstemci bağlantıyı kapattı.");
//                        break;
//                    }

//                    var dataSlice = buffer[..bytesRead];

//                    if (dataSlice == null || dataSlice.Length == 0)
//                    {
//                        Console.WriteLine("Boş veri alındı, atlanıyor.");
//                        continue;
//                    }

//                    var command = new ReadDataFromTCMSWithTCPCommand
//                    {
//                        DataBytes = dataSlice,
//                        RemoteEndPoint = remoteEndPoint
//                    };

//                    var result = await mediator.Send(command, stoppingToken);

//                    if (result is not null)
//                    {
//                        await mediator.Send(new SendCoupledDataToCoupleExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                        await mediator.Send(new SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                    }
//                    else
//                    {
//                        Console.WriteLine("İşlenen veri null döndü.");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Client işlem hatası: {ex.Message}");
//            }
//            finally
//            {
//                client.Close();
//            }
//        }

//    }
//}





//İKİ FARKLI VERİ OKUMA YÖNTEMİ İLE YAPILDI VERİNİN NERDE BAŞLAYIP BİTTİĞİNİN BELİRLENMESİ GEREK JSON PARSİNG SORUNLARI OLUŞABİLİYOR ÜSTTEKİ KODDA


//using MediatR;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.IO;
//using Tram34TCMSInterface.Application.Features.ReadDataFromTCMSWithTCP;
//using Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS;
//using Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS;

//namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
//{
//    public class ReadDataFromTCMSWithTCPBackgroundService : BackgroundService
//    {
//        private readonly IMediator mediator;
//        private readonly TcpListener listener;

//        public ReadDataFromTCMSWithTCPBackgroundService(IMediator mediator, IConfiguration configuration)
//        {
//            this.mediator = mediator;
//            string? ip = configuration["TCP:Address"];
//            string? portStr = configuration["TCP:SourcePort"];

//            if (string.IsNullOrWhiteSpace(ip))
//                throw new ArgumentException("TCP IP address cannot be null or empty. Check configuration [TCP:Address]");

//            if (!int.TryParse(portStr, out int port) || port <= 0 || port > 65535)
//                throw new ArgumentException("TCP port is invalid. Check configuration [TCP:SourcePort]");

//            if (!IPAddress.TryParse(ip, out IPAddress? ipAddress))
//                throw new ArgumentException("TCP IP address format is invalid. Check configuration [TCP:Address]");

//            listener = new TcpListener(ipAddress, port);
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            Console.WriteLine("TCP Background Service Başladı...");

//            listener.Start();

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    var client = await listener.AcceptTcpClientAsync(stoppingToken);
//                    _ = Task.Run(() => HandleClientAsync(client, stoppingToken), stoppingToken); // fire and forget
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Bağlantı alma hatası: {ex.Message}");
//                }
//            }
//        }

//        private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
//        {
//            Console.WriteLine($"Yeni bağlantı: {client.Client.RemoteEndPoint}");

//            using var stream = client.GetStream();
//            using var reader = new StreamReader(stream, Encoding.UTF8); // SATIR OKUMA
//            var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

//            try
//            {
//                while (!stoppingToken.IsCancellationRequested && client.Connected)
//                {
//                    string? jsonLine = await reader.ReadLineAsync();

//                    if (string.IsNullOrWhiteSpace(jsonLine))
//                    {
//                        Console.WriteLine("Boş satır alındı, atlanıyor.");
//                        continue;
//                    }

//                    byte[] dataBytes = Encoding.UTF8.GetBytes(jsonLine); // Eğer handler hala byte bekliyorsa
//                    var command = new ReadDataFromTCMSWithTCPCommand
//                    {
//                        DataBytes = dataBytes,
//                        RemoteEndPoint = remoteEndPoint
//                    };

//                    var result = await mediator.Send(command, stoppingToken);

//                    if (result is not null)
//                    {
//                        await mediator.Send(new SendCoupledDataToCoupleExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                        await mediator.Send(new SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                    }
//                    else
//                    {
//                        Console.WriteLine("İşlenen veri null döndü.");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Client işlem hatası: {ex.Message}");
//            }
//            finally
//            {
//                client.Close();
//               // Console.WriteLine($"Bağlantı kapandı: {client.Client.RemoteEndPoint}");
//            }
//        }
//    }
//}


//TCMS CLİENT SİDE KISMI ÇALIŞAN TARAF


//using MediatR;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using Tram34TCMSInterface.Application.Features.ReadDataFromTCMSWithTCP;
//using Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS;
//using Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS;

//namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
//{
//    public class ReadDataFromTCMSWithTCPBackgroundService : BackgroundService
//    {
//        private readonly IMediator mediator;
//        private readonly IConfiguration configuration;
//        private TcpClient _client;
//        private NetworkStream _stream;

//        public ReadDataFromTCMSWithTCPBackgroundService(IMediator mediator, IConfiguration configuration)
//        {
//            this.mediator = mediator;
//            this.configuration = configuration;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            string? ip = configuration["TCP:Address"]; //bağlanılacak server ıp 
//            string? portStr = configuration["TCP:SourcePort"]; //bağlanılacak server port
//            string? localIpStr = configuration["TCP:LocalIP"];
//            string? localPortStr = configuration["TCP:LocalPort"]; // bind edilecek port

//            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(localIpStr))
//                throw new ArgumentException("TCP IP adresleri boş olamaz. [TCP:Address], [TCP:LocalIP]");

//            if (!int.TryParse(portStr, out int remotePort) || remotePort <= 0 || remotePort > 65535)
//                throw new ArgumentException("Uzak TCP portu hatalı. [TCP:SourcePort]");

//            if (!int.TryParse(localPortStr, out int localPort) || localPort <= 0 || localPort > 65535)
//                throw new ArgumentException("Yerel TCP portu hatalı. [TCP:LocalPort]");

//            if (!IPAddress.TryParse(ip, out var remoteIPAddress))
//                throw new ArgumentException("Uzak IP formatı hatalı. [TCP:Address]");

//            if (!IPAddress.TryParse(localIpStr, out var localIPAddress))
//                throw new ArgumentException("Yerel IP formatı hatalı. [TCP:LocalIP]");

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    // Socket oluştur ve bind et
//                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
//                    socket.Bind(new IPEndPoint(localIPAddress, localPort));

//                    _client = new TcpClient { Client = socket };

//                    await _client.ConnectAsync(remoteIPAddress, remotePort, stoppingToken);
//                    _stream = _client.GetStream();

//                    Console.WriteLine($"TCP istemci bağlantısı kuruldu. Yerel: {localIPAddress}:{localPort} → Sunucu: {remoteIPAddress}:{remotePort}");

//                    await HandleServerAsync(_client, _stream, stoppingToken);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Sunucuya bağlanılamadı: {ex.Message}");
//                    await Task.Delay(2000, stoppingToken);
//                }
//            }
//        }

//        private async Task HandleServerAsync(TcpClient client, NetworkStream stream, CancellationToken stoppingToken)
//        {
//            var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

//            try
//            {
//                byte[] buffer = new byte[2048];

//                while (!stoppingToken.IsCancellationRequested && client.Connected)
//                {
//                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);

//                    if (bytesRead == 0)
//                    {
//                        Console.WriteLine("Sunucu bağlantıyı kapattı.");
//                        break;
//                    }

//                    var dataSlice = buffer[..bytesRead];

//                    if (dataSlice == null || dataSlice.Length == 0)
//                    {
//                        Console.WriteLine("Boş veri alındı, atlanıyor.");
//                        continue;
//                    }

//                    var command = new ReadDataFromTCMSWithTCPCommand
//                    {
//                        DataBytes = dataSlice,
//                        RemoteEndPoint = remoteEndPoint
//                    };

//                    var result = await mediator.Send(command, stoppingToken);

//                    if (result is not null)
//                    {
//                        await mediator.Send(new SendCoupledDataToCoupleExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                        await mediator.Send(new SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                    }
//                    else
//                    {
//                        Console.WriteLine("İşlenen veri null döndü.");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Sunucudan gelen veri işleme hatası: {ex.Message}");
//            }
//            finally
//            {
//                client.Close();
//            }
//        }

//        public override void Dispose()
//        {
//            _client?.Close();
//            base.Dispose();
//        }
//    }
//}



//CLİENT READERLİNE ASYNC çalışıyor

//using MediatR;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.IO;
//using Tram34TCMSInterface.Application.Features.ReadDataFromTCMSWithTCP;
//using Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS;
//using Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS;

//namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
//{
//    public class ReadDataFromTCMSWithTCPBackgroundService : BackgroundService
//    {
//        private readonly IMediator mediator;
//        private readonly IConfiguration configuration;
//        private TcpClient _client;
//        private NetworkStream _stream;

//        public ReadDataFromTCMSWithTCPBackgroundService(IMediator mediator, IConfiguration configuration)
//        {
//            this.mediator = mediator;
//            this.configuration = configuration;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            string? ip = configuration["TCP:Address"];
//            string? portStr = configuration["TCP:SourcePort"];
//            string? localIpStr = configuration["TCP:LocalIP"];
//            string? localPortStr = configuration["TCP:LocalPort"];

//            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(localIpStr))
//                throw new ArgumentException("TCP IP adresleri boş olamaz. [TCP:Address], [TCP:LocalIP]");

//            if (!int.TryParse(portStr, out int remotePort) || remotePort <= 0 || remotePort > 65535)
//                throw new ArgumentException("Uzak TCP portu hatalı. [TCP:SourcePort]");

//            if (!int.TryParse(localPortStr, out int localPort) || localPort <= 0 || localPort > 65535)
//                throw new ArgumentException("Yerel TCP portu hatalı. [TCP:LocalPort]");

//            if (!IPAddress.TryParse(ip, out var remoteIPAddress))
//                throw new ArgumentException("Uzak IP formatı hatalı. [TCP:Address]");

//            if (!IPAddress.TryParse(localIpStr, out var localIPAddress))
//                throw new ArgumentException("Yerel IP formatı hatalı. [TCP:LocalIP]");

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
//                    socket.Bind(new IPEndPoint(localIPAddress, localPort));

//                    _client = new TcpClient { Client = socket };

//                    await _client.ConnectAsync(remoteIPAddress, remotePort, stoppingToken);
//                    _stream = _client.GetStream();

//                    Console.WriteLine($"TCP istemci bağlantısı kuruldu. Yerel: {localIPAddress}:{localPort} → Sunucu: {remoteIPAddress}:{remotePort}");

//                    await HandleServerAsync(_client, _stream, stoppingToken);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Sunucuya bağlanılamadı: {ex.Message}");
//                    await Task.Delay(2000, stoppingToken);
//                }
//            }
//        }

//        private async Task HandleServerAsync(TcpClient client, NetworkStream stream, CancellationToken stoppingToken)
//        {
//            var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

//            try
//            {
//                using var reader = new StreamReader(stream, Encoding.UTF8);

//                while (!stoppingToken.IsCancellationRequested && client.Connected)
//                {
//                    string? line = await reader.ReadLineAsync();

//                    if (string.IsNullOrWhiteSpace(line))
//                        continue;

//                    byte[] dataSlice = Encoding.UTF8.GetBytes(line);

//                    var command = new ReadDataFromTCMSWithTCPCommand
//                    {
//                        DataBytes = dataSlice,
//                        RemoteEndPoint = remoteEndPoint
//                    };

//                    var result = await mediator.Send(command, stoppingToken);

//                    if (result is not null)
//                    {
//                        await mediator.Send(new SendCoupledDataToCoupleExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                        await mediator.Send(new SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                    }
//                    else
//                    {
//                        Console.WriteLine("İşlenen veri null döndü.");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Sunucudan gelen veri işleme hatası: {ex.Message}");
//            }
//            finally
//            {
//                client.Close();
//            }
//        }

//        public override void Dispose()
//        {
//            _client?.Close();
//            base.Dispose();
//        }
//    }
//}

//\n gelene kadar gelen verileri biriktirir ama ram şişebilir çözüm bul ram şişmesi yok kodu değiştirdim bu kod normal çalışıyor
//using MediatR;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.IO;
//using Tram34TCMSInterface.Application.Features.ReadDataFromTCMSWithTCP;
//using Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS;
//using Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS;
//using System.Text.Json;
//using Tram34TCMSInterface.Domain.Models;
//using System.Text.Json.Serialization;

//namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
//{
//    public class ReadDataFromTCMSWithTCPBackgroundService : BackgroundService
//    {
//        private readonly IMediator mediator;
//        private readonly IConfiguration configuration;
//        private TcpClient _client;
//        private NetworkStream _stream;
//        private bool isConnected = false;

//        public ReadDataFromTCMSWithTCPBackgroundService(IMediator mediator, IConfiguration configuration)
//        {
//            this.mediator = mediator;
//            this.configuration = configuration;
//        }


//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            string? ip = configuration["TCP:Address"];
//            string? portStr = configuration["TCP:SourcePort"];
//            string? localIpStr = configuration["TCP:LocalIP"];
//            string? localPortStr = configuration["TCP:LocalPort"];

//            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(localIpStr))
//                throw new ArgumentException("TCP IP adresleri boş olamaz. [TCP:Address], [TCP:LocalIP]");

//            if (!int.TryParse(portStr, out int remotePort) || remotePort <= 0 || remotePort > 65535)
//                throw new ArgumentException("Uzak TCP portu hatalı. [TCP:SourcePort]");

//            if (!int.TryParse(localPortStr, out int localPort) || localPort <= 0 || localPort > 65535)
//                throw new ArgumentException("Yerel TCP portu hatalı. [TCP:LocalPort]");

//            if (!IPAddress.TryParse(ip, out var remoteIPAddress))
//                throw new ArgumentException("Uzak IP formatı hatalı. [TCP:Address]");

//            if (!IPAddress.TryParse(localIpStr, out var localIPAddress))
//                throw new ArgumentException("Yerel IP formatı hatalı. [TCP:LocalIP]");

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {

//                    if (!isConnected)
//                    {


//                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
//                    socket.Bind(new IPEndPoint(localIPAddress, localPort));
//                    //flag yapısı koyulacak
//                    _client = new TcpClient { Client = socket };

//                    await _client.ConnectAsync(remoteIPAddress, remotePort, stoppingToken);
//                    _stream = _client.GetStream();

//                    Console.WriteLine($"TCP istemci bağlantısı kuruldu. Yerel: {localIPAddress}:{localPort} → Sunucu: {remoteIPAddress}:{remotePort}");
//                    isConnected = true;
//                    }
//                    _ = Task.Run(() => HandleWriteAsync(_stream, stoppingToken), stoppingToken);

//                    await HandleServerAsync(_client, _stream, stoppingToken);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Sunucuya bağlanılamadı: {ex.Message}");
//                    await Task.Delay(2000, stoppingToken);
//                }
//            }
//        }




//        private bool HasNullProperty(object obj)
//        {
//            if (obj == null) return true;

//            var type = obj.GetType();
//            foreach (var property in type.GetProperties())
//            {
//                // CouplingTrainsId dışındakiler kontrol edilsin
//                if (property.Name == nameof(JsonDocumentFormatUDP.TrainData.CouplingTrainsId))
//                    continue;

//                var value = property.GetValue(obj);

//                if (value == null)
//                    return true;

//                // Eğer alt nesne ise, onun içinde de null kontrolü yapılabilir
//                if (!property.PropertyType.IsPrimitive && property.PropertyType != typeof(string))
//                {
//                    if (HasNullProperty(value))
//                        return true;
//                }
//            }
//            return false;
//        }


//        private async Task HandleServerAsync(TcpClient client, NetworkStream stream, CancellationToken stoppingToken)
//        {
//            var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

//            try
//            {
//                const int LENGTH_HEADER_SIZE = 4;
//                var lengthBuffer = new byte[LENGTH_HEADER_SIZE];

//                while (!stoppingToken.IsCancellationRequested && client.Connected)
//                {
//                    // 1) 4 byte uzunluk bilgisini oku
//                    int totalRead = 0;
//                    while (totalRead < LENGTH_HEADER_SIZE)
//                    {
//                        int bytesRead = await stream.ReadAsync(lengthBuffer.AsMemory(totalRead, LENGTH_HEADER_SIZE - totalRead), stoppingToken);
//                        if (bytesRead == 0)
//                            throw new IOException("Bağlantı kapandı, uzunluk bilgisi alınamadı.");
//                        totalRead += bytesRead;
//                    }

//                    // BigEndian 4 byte int olarak uzunluğu oku
//                    int messageLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
//                    if (messageLength != 1186)
//                        throw new InvalidDataException($"Geçersiz mesaj uzunluğu: {messageLength}");

//                    // 2) Mesajın tamamını oku (JSON)
//                    using var ms = new MemoryStream();
//                    int remaining = messageLength;
//                    while (remaining > 0)
//                    {
//                        var buffer = new byte[Math.Min(1186, remaining)];
//                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);
//                        if (bytesRead == 0)
//                            throw new IOException("Bağlantı kapandı, mesaj tamamlanamadı.");

//                        await ms.WriteAsync(buffer.AsMemory(0, bytesRead), stoppingToken);
//                        remaining -= bytesRead;
//                    }

//                    // 3) \n karakterini oku ve kontrol et (async)
//                    var newlineBuffer = new byte[1];
//                    int readNewline = await stream.ReadAsync(newlineBuffer, 0, 1, stoppingToken);
//                    if (readNewline == 0 || newlineBuffer[0] != (byte)'\n')
//                    {
//                        Console.WriteLine("Uyarı: Beklenen '\\n' karakteri bulunamadı.");
//                        continue;
//                    }

//                    // 4) JSON string olarak decode et
//                    string jsonString = Encoding.UTF8.GetString(ms.ToArray());

//                    if (string.IsNullOrWhiteSpace(jsonString))
//                        continue;

//                    var options = new JsonSerializerOptions
//                    {
//                        PropertyNameCaseInsensitive = true,
//                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
//                    };

//                    var trainData = JsonSerializer.Deserialize<JsonDocumentFormatUDP.TrainData>(jsonString, options);

//                    if (trainData == null || HasNullProperty(trainData))
//                    {
//                        Console.WriteLine("Uyarı: JSON deserialization başarısız veya null alan içeriyor.");
//                        continue;
//                    }

//                    byte[] dataSlice = Encoding.UTF8.GetBytes(jsonString);

//                    var command = new ReadDataFromTCMSWithTCPCommand
//                    {
//                        DataBytes = dataSlice,
//                        RemoteEndPoint = remoteEndPoint
//                    };

//                    var result = await mediator.Send(command, stoppingToken);



//                    if (result is not null)
//                    {
//                        await mediator.Send(new SendCoupledDataToCoupleExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                        await mediator.Send(new SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                    }
//                    else
//                    {
//                        Console.WriteLine("İşlenen veri null döndü.");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Sunucudan gelen veri işleme hatası: {ex.Message}");
//                isConnected = false;
//                 client.Close();
//            }
//            //finally
//            //{
//            //     client.Dispose();
//            //}
//        }


//        private string GetLocalIPAddress()
//        {
//            var host = Dns.GetHostEntry(Dns.GetHostName());
//            foreach (var ip in host.AddressList)
//            {
//                if (ip.AddressFamily == AddressFamily.InterNetwork)
//                {
//                    return ip.ToString();
//                }
//            }
//            return "127.0.0.1"; // fallback
//        }



//        private async Task HandleWriteAsync(NetworkStream stream, CancellationToken cancellationToken)
//        {
//            while (!cancellationToken.IsCancellationRequested)
//            {
//                var now = DateTime.UtcNow;

//                var outgoingData = new
//                {
//                    TimeStamp = now.ToString("yyyy-MM-ddTHH:mm:ss"),
//                    Date = now.ToString("yyyy-MM-dd"),
//                    Time = now.ToString("HH:mm:ss"),
//                    IP = GetLocalIPAddress(),
//                    Heartbeat = 1,
//                    YBS_Announcement_State = true,
//                    YBS_Intercom_State = true,
//                    YBS_Intercom_1 = false,
//                    YBS_Intercom_2 = false,
//                    YBS_Intercom_3 = false,
//                    YBS_Intercom_4 = false,
//                    YBS_Intercom_5 = false,
//                    YBS_Intercom_6 = false,
//                    YBS_Intercom_7 = false,
//                    YBS_Intercom_8 = false
//                };

//                var json = JsonSerializer.Serialize(outgoingData);
//                var bytes = Encoding.UTF8.GetBytes(json);

//                try
//                {
//                    await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
//                    Console.WriteLine("Gönderilen JSON: " + json);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine("Veri gönderme hatası: " + ex.Message);
//                    break; // Bağlantı koptuysa çık
//                }

//                await Task.Delay(500, cancellationToken); // 500 ms aralıkla gönderim
//            }
//        }




//    }
//}

//ÇALIŞIYOR DÜZGÜN VERİDE


//using MediatR;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.IO;
//using Tram34TCMSInterface.Application.Features.ReadDataFromTCMSWithTCP;
//using Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS;
//using Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS;
//using System.Text.Json;
//using Tram34TCMSInterface.Domain.Models;
//using System.Text.Json.Serialization;

//namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
//{
//    public class ReadDataFromTCMSWithTCPBackgroundService : BackgroundService
//    {
//        private readonly IMediator mediator;
//        private readonly IConfiguration configuration;
//        private TcpClient _client;
//        private NetworkStream _stream;
//        private bool isConnected = false;

//        public ReadDataFromTCMSWithTCPBackgroundService(IMediator mediator, IConfiguration configuration)
//        {
//            this.mediator = mediator;
//            this.configuration = configuration;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            string? ip = configuration["TCP:Address"];
//            string? portStr = configuration["TCP:SourcePort"];
//            string? localIpStr = configuration["TCP:LocalIP"];
//            string? localPortStr = configuration["TCP:LocalPort"];

//            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(localIpStr))
//                throw new ArgumentException("TCP IP adresleri boş olamaz.");

//            if (!int.TryParse(portStr, out int remotePort) || !int.TryParse(localPortStr, out int localPort))
//                throw new ArgumentException("TCP port bilgileri hatalı.");

//            if (!IPAddress.TryParse(ip, out var remoteIPAddress) || !IPAddress.TryParse(localIpStr, out var localIPAddress))
//                throw new ArgumentException("IP adres formatı hatalı.");

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    if (!isConnected || _client == null || !_client.Connected || !IsSocketConnected(_client.Client))
//                    {
//                        CleanupConnection();
//                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
//                        socket.Bind(new IPEndPoint(localIPAddress, localPort));

//                        _client = new TcpClient { Client = socket };
//                        await _client.ConnectAsync(remoteIPAddress, remotePort, stoppingToken);
//                        _stream = _client.GetStream();
//                        isConnected = true;

//                        Console.WriteLine($"Bağlandı: {localIPAddress}:{localPort} → {remoteIPAddress}:{remotePort}");
//                    }

//                    _ = Task.Run(() => HandleWriteAsync(_stream, stoppingToken), stoppingToken);

//                    await HandleServerAsync(_client, _stream, stoppingToken);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Bağlantı hatası: {ex.Message}");
//                    CleanupConnection();
//                    await Task.Delay(4000, stoppingToken);
//                }
//            }
//        }

//        private bool IsSocketConnected(Socket socket)
//        {
//            try
//            {
//                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        private void CleanupConnection()
//        {
//            try { _stream?.Close(); } catch { }
//            try { _stream?.Dispose(); } catch { }

//            try
//            {
//                if (_client?.Client?.Connected == true)
//                {
//                    _client.Client.Shutdown(SocketShutdown.Both);
//                }
//            }
//            catch { }

//            try { _client?.Close(); } catch { }
//            try { _client?.Dispose(); } catch { }

//            _stream = null;
//            _client = null;
//            isConnected = false;
//        }


//        private async Task HandleServerAsync(TcpClient client, NetworkStream stream, CancellationToken stoppingToken)
//        {
//            try
//            {
//                var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
//                const int LENGTH_HEADER_SIZE = 4;
//                var lengthBuffer = new byte[LENGTH_HEADER_SIZE];

//                while (!stoppingToken.IsCancellationRequested && client.Connected)
//                {
//                    int totalRead = 0;
//                    while (totalRead < LENGTH_HEADER_SIZE)
//                    {
//                        int bytesRead = await stream.ReadAsync(lengthBuffer.AsMemory(totalRead, LENGTH_HEADER_SIZE - totalRead), stoppingToken);
//                        if (bytesRead == 0)
//                            throw new IOException("Uzunluk alınamadı.");
//                        totalRead += bytesRead;
//                    }

//                    int messageLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
//                    if (messageLength <= 0 || messageLength > 1024 * 1024)
//                        throw new InvalidDataException($"Geçersiz uzunluk: {messageLength}");

//                    using var ms = new MemoryStream();
//                    int remaining = messageLength;
//                    while (remaining > 0)
//                    {
//                        var buffer = new byte[Math.Min(2048, remaining)];
//                        int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), stoppingToken);
//                        if (bytesRead == 0)
//                            throw new IOException("Mesaj tamamlanamadı.");

//                        await ms.WriteAsync(buffer.AsMemory(0, bytesRead), stoppingToken);
//                        remaining -= bytesRead;
//                    }

//                    // \n kontrolü
//                    var newlineBuffer = new byte[1];
//                    int readNewline = await stream.ReadAsync(newlineBuffer, 0, 1, stoppingToken);
//                    if (readNewline == 0 || newlineBuffer[0] != (byte)'\n')
//                    {
//                        Console.WriteLine("Uyarı: \\n eksik veya hatalı.");
//                        throw new IOException("Mesaj tamamlanamadı.");
//                        //continue;
//                    }

//                    var jsonString = Encoding.UTF8.GetString(ms.ToArray());
//                    if (string.IsNullOrWhiteSpace(jsonString))
//                        continue;

//                    var options = new JsonSerializerOptions
//                    {
//                        PropertyNameCaseInsensitive = true,
//                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
//                    };

//                    var trainData = JsonSerializer.Deserialize<JsonDocumentFormatUDP.TrainData>(jsonString, options);

//                    if (trainData == null || HasNullProperty(trainData))
//                    {
//                        Console.WriteLine("Uyarı: Null alanlı veya hatalı JSON.");
//                        throw new IOException("Mesaj tamamlanamadı.");
//                        //continue;
//                    }

//                    var command = new ReadDataFromTCMSWithTCPCommand
//                    {
//                        DataBytes = Encoding.UTF8.GetBytes(jsonString),
//                        RemoteEndPoint = remoteEndPoint
//                    };

//                    var result = await mediator.Send(command, stoppingToken);
//                    if (result is not null)
//                    {
//                        await mediator.Send(new SendCoupledDataToCoupleExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                        await mediator.Send(new SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Veri işleme hatası: {ex.Message}");
//                CleanupConnection();
//            }
//        }

//        private bool HasNullProperty(object obj)
//        {
//            if (obj == null) return true;
//            var type = obj.GetType();
//            foreach (var property in type.GetProperties())
//            {
//                if (property.Name == nameof(JsonDocumentFormatUDP.TrainData.CouplingTrainsId)) continue;
//                var value = property.GetValue(obj);
//                if (value == null) return true;
//                if (!property.PropertyType.IsPrimitive && property.PropertyType != typeof(string))
//                    if (HasNullProperty(value)) return true;
//            }
//            return false;
//        }

//        private string GetLocalIPAddress()
//        {
//            var host = Dns.GetHostEntry(Dns.GetHostName());
//            foreach (var ip in host.AddressList)
//                if (ip.AddressFamily == AddressFamily.InterNetwork)
//                    return ip.ToString();
//            return "127.0.0.1";
//        }

//        private async Task HandleWriteAsync(NetworkStream stream, CancellationToken cancellationToken)
//        {
//            while (!cancellationToken.IsCancellationRequested)
//            {
//                var now = DateTime.UtcNow;

//                var outgoingData = new
//                {
//                    TimeStamp = now.ToString("yyyy-MM-ddTHH:mm:ss"),
//                    Date = now.ToString("yyyy-MM-dd"),
//                    Time = now.ToString("HH:mm:ss"),
//                    IP = GetLocalIPAddress(),
//                    Heartbeat = 1,
//                    YBS_Announcement_State = true,
//                    YBS_Intercom_State = true,
//                    YBS_Intercom_1 = false,
//                    YBS_Intercom_2 = false,
//                    YBS_Intercom_3 = false,
//                    YBS_Intercom_4 = false,
//                    YBS_Intercom_5 = false,
//                    YBS_Intercom_6 = false,
//                    YBS_Intercom_7 = false,
//                    YBS_Intercom_8 = false
//                };

//                var json = JsonSerializer.Serialize(outgoingData);
//                var bytes = Encoding.UTF8.GetBytes(json);

//                try
//                {
//                    await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
//                    Console.WriteLine("Gönderildi: " + json);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine("Gönderim hatası: " + ex.Message);
//                    break;
//                }

//                await Task.Delay(500, cancellationToken);
//            }
//        }
//    }
//}
//bu kod da çalışıyor
//using MediatR;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.IO;
//using Tram34TCMSInterface.Application.Features.ReadDataFromTCMSWithTCP;
//using Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS;
//using Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS;
//using System.Text.Json;
//using Tram34TCMSInterface.Domain.Models;
//using System.Text.Json.Serialization;

//namespace Tram34TCMSInterface.Infrastructure.BackgroundServices;

//public class ReadDataFromTCMSWithTCPBackgroundService : BackgroundService
//{
//    private readonly IMediator mediator;
//    private readonly IConfiguration configuration;
//    private TcpClient _client;
//    private NetworkStream _stream;
//    private bool isConnected = false;

//    public ReadDataFromTCMSWithTCPBackgroundService(IMediator mediator, IConfiguration configuration)
//    {
//        this.mediator = mediator;
//        this.configuration = configuration;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        string ip = configuration["TCP:Address"];
//        string portStr = configuration["TCP:SourcePort"];
//        string localIpStr = configuration["TCP:LocalIP"];
//        string localPortStr = configuration["TCP:LocalPort"];

//        if (!int.TryParse(portStr, out int remotePort) || !int.TryParse(localPortStr, out int localPort))
//            throw new ArgumentException("Port bilgileri hatalı.");

//        if (!IPAddress.TryParse(ip, out var remoteIPAddress) || !IPAddress.TryParse(localIpStr, out var localIPAddress))
//            throw new ArgumentException("IP adres formatı hatalı.");

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                if (!isConnected || _client == null || !_client.Connected || !IsSocketConnected(_client.Client))
//                {
//                    CleanupConnection();

//                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
//                    socket.Bind(new IPEndPoint(localIPAddress, localPort));

//                    _client = new TcpClient { Client = socket };
//                    await _client.ConnectAsync(remoteIPAddress, remotePort, stoppingToken);
//                    _stream = _client.GetStream();
//                    isConnected = true;

//                    Console.WriteLine($"Bağlandı: {localIPAddress}:{localPort} → {remoteIPAddress}:{remotePort}");
//                }

//                _ = Task.Run(() => HandleWriteAsync(_stream, stoppingToken), stoppingToken);
//                await HandleServerAsync(_client, _stream, stoppingToken);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Bağlantı hatası: {ex.Message}");
//                await Task.Delay(2000, stoppingToken);
//            }
//        }
//    }

//    private bool IsSocketConnected(Socket socket)
//    {
//        try { return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0); }
//        catch { return false; }
//    }

//    private void CleanupConnection()
//    {
//        try { _stream?.Close(); } catch { }
//        try { _stream?.Dispose(); } catch { }

//        try { if (_client?.Client?.Connected == true) _client.Client.Shutdown(SocketShutdown.Both); } catch { }

//        try { _client?.Close(); _client?.Dispose(); } catch { }

//        _stream = null;
//        _client = null;
//        isConnected = false;
//    }

//    private async Task HandleServerAsync(TcpClient client, NetworkStream stream, CancellationToken stoppingToken)
//    {
//        const int LENGTH_HEADER_SIZE = 4;
//        var lengthBuffer = new byte[LENGTH_HEADER_SIZE];
//        var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

//        try
//        {
//            while (!stoppingToken.IsCancellationRequested && client.Connected)
//            {
//                // 1) Mesaj uzunluğunu oku
//                if (!await ReadExactAsync(stream, lengthBuffer, 0, LENGTH_HEADER_SIZE, stoppingToken))
//                    throw new IOException("Mesaj uzunluğu alınamadı.");

//                int messageLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
//                if (messageLength <= 0 || messageLength > 1024 * 1024)
//                    throw new InvalidDataException($"Geçersiz uzunluk: {messageLength}");

//                // 2) Mesaj gövdesini oku
//                byte[] bodyBuffer = new byte[messageLength];
//                if (!await ReadExactAsync(stream, bodyBuffer, 0, messageLength, stoppingToken))
//                {
//                    Console.WriteLine("Mesaj gövdesi eksik geldi. Temizleniyor...");
//                    await DiscardUntilNewlineAsync(stream, stoppingToken);
//                    continue;
//                }

//                // 3) \n kontrolü
//                int newline = stream.ReadByte();
//                if (newline != '\n')
//                {
//                    Console.WriteLine("Uyarı: Mesaj sonu '\\n' eksik. Temizleniyor...");
//                    await DiscardUntilNewlineAsync(stream, stoppingToken);
//                    continue;
//                }

//                string jsonString = Encoding.UTF8.GetString(bodyBuffer);
//                if (string.IsNullOrWhiteSpace(jsonString)) continue;

//                var options = new JsonSerializerOptions
//                {
//                    PropertyNameCaseInsensitive = true,
//                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
//                };

//                var trainData = JsonSerializer.Deserialize<JsonDocumentFormatUDP.TrainData>(jsonString, options);
//                Console.WriteLine(jsonString);
//                if (trainData == null || HasNullProperty(trainData))
//                {
//                    Console.WriteLine("Uyarı: Hatalı JSON. Atlandı.");
//                    continue;
//                }

//                var command = new ReadDataFromTCMSWithTCPCommand
//                {
//                    DataBytes = bodyBuffer,
//                    RemoteEndPoint = remoteEndPoint
//                };

//                var result = await mediator.Send(command, stoppingToken);
//                if (result is not null)
//                {
//                    await mediator.Send(new SendCoupledDataToCoupleExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                    await mediator.Send(new SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Veri işleme hatası: {ex.Message}");
//            CleanupConnection();
//        }
//    }

//    private async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, int offset, int length, CancellationToken ct)
//    {
//        int totalRead = 0;
//        while (totalRead < length)
//        {
//            int bytesRead = await stream.ReadAsync(buffer.AsMemory(offset + totalRead, length - totalRead), ct);
//            if (bytesRead == 0) return false;
//            totalRead += bytesRead;
//        }
//        return true;
//    }

//    private async Task DiscardUntilNewlineAsync(Stream stream, CancellationToken ct)
//    {
//        byte[] discardBuffer = new byte[1];
//        int discardCount = 0;

//        while (!ct.IsCancellationRequested)
//        {
//            int read = await stream.ReadAsync(discardBuffer, 0, 1, ct);
//            if (read == 0) break;
//            if (discardBuffer[0] == (byte)'\n') break;
//            discardCount++;
//            if (discardCount > 1024) break; // güvenlik sınırı
//        }

//        Console.WriteLine($"Discard edildi: {discardCount} byte");
//    }

//    private bool HasNullProperty(object obj)
//    {
//        if (obj == null) return true;
//        var type = obj.GetType();
//        foreach (var property in type.GetProperties())
//        {
//            if (property.Name == nameof(JsonDocumentFormatUDP.TrainData.CouplingTrainsId)) continue;
//            var value = property.GetValue(obj);
//            if (value == null) return true;
//            if (!property.PropertyType.IsPrimitive && property.PropertyType != typeof(string))
//                if (HasNullProperty(value)) return true;
//        }
//        return false;
//    }

//    private string GetLocalIPAddress()
//    {
//        var host = Dns.GetHostEntry(Dns.GetHostName());
//        return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
//    }

//    private async Task HandleWriteAsync(NetworkStream stream, CancellationToken cancellationToken)
//    {
//        while (!cancellationToken.IsCancellationRequested)
//        {
//            var now = DateTime.UtcNow;

//            var outgoingData = new
//            {
//                TimeStamp = now.ToString("yyyy-MM-ddTHH:mm:ss"),
//                Date = now.ToString("yyyy-MM-dd"),
//                Time = now.ToString("HH:mm:ss"),
//                IP = GetLocalIPAddress(),
//                Heartbeat = 1,
//                YBS_Announcement_State = true,
//                YBS_Intercom_State = true,
//                YBS_Intercom_1 = false,
//                YBS_Intercom_2 = false,
//                YBS_Intercom_3 = false,
//                YBS_Intercom_4 = false,
//                YBS_Intercom_5 = false,
//                YBS_Intercom_6 = false,
//                YBS_Intercom_7 = false,
//                YBS_Intercom_8 = false
//            };

//            var json = JsonSerializer.Serialize(outgoingData);
//            var bytes = Encoding.UTF8.GetBytes(json);

//            try
//            {
//                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
//                Console.WriteLine("Gönderildi: " + json);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Gönderim hatası: " + ex.Message);
//                break;
//            }

//            await Task.Delay(500, cancellationToken);
//        }
//    }
//}

//using MediatR;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Text.Json;
//using Tram34TCMSInterface.Application.Features.ReadDataFromTCMSWithTCP;
//using Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS;
//using Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS;
//using Tram34TCMSInterface.Domain.Models;
//using System.Text.Json.Serialization;

//namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
//{
//    public class ReadDataFromTCMSWithTCPBackgroundService : BackgroundService
//    {
//        private readonly IMediator mediator;
//        private readonly IConfiguration configuration;
//        private TcpClient _client;
//        private NetworkStream _stream;
//        private bool isConnected = false;

//        public ReadDataFromTCMSWithTCPBackgroundService(IMediator mediator, IConfiguration configuration)
//        {
//            this.mediator = mediator;
//            this.configuration = configuration;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            string? ip = configuration["TCP:Address"];
//            string? portStr = configuration["TCP:SourcePort"];
//            string? localIpStr = configuration["TCP:LocalIP"];
//            string? localPortStr = configuration["TCP:LocalPort"];

//            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(localIpStr))
//                throw new ArgumentException("TCP IP adresleri boş olamaz.");

//            if (!int.TryParse(portStr, out int remotePort) || !int.TryParse(localPortStr, out int localPort))
//                throw new ArgumentException("TCP port bilgileri hatalı.");

//            if (!IPAddress.TryParse(ip, out var remoteIPAddress) || !IPAddress.TryParse(localIpStr, out var localIPAddress))
//                throw new ArgumentException("IP adres formatı hatalı.");

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    if (!isConnected || _client == null || !_client.Connected || !IsSocketConnected(_client.Client))
//                    {
//                        CleanupConnection();

//                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
//                        socket.Bind(new IPEndPoint(localIPAddress, localPort));

//                        _client = new TcpClient { Client = socket };
//                        await _client.ConnectAsync(remoteIPAddress, remotePort, stoppingToken);
//                        _stream = _client.GetStream();
//                        isConnected = true;

//                        Console.WriteLine($"Bağlandı: {localIPAddress}:{localPort} → {remoteIPAddress}:{remotePort}");

//                        // Yazma görevini bir kez başlat
//                        _ = Task.Run(() => HandleWriteAsync(_stream, stoppingToken), stoppingToken);
//                    }

//                    await HandleServerAsync(_client, _stream, stoppingToken);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Bağlantı hatası: {ex.Message}");
//                    CleanupConnection();
//                    await Task.Delay(500, stoppingToken);
//                }
//            }
//        }

//        private bool IsSocketConnected(Socket socket)
//        {
//            try
//            {
//                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        private void CleanupConnection()
//        {
//            try { _stream?.Close(); } catch { }
//            try { _stream?.Dispose(); } catch { }

//            try
//            {
//                if (_client?.Client?.Connected == true)
//                {
//                    _client.Client.Shutdown(SocketShutdown.Both);
//                }
//            }
//            catch { }

//            try { _client?.Close(); } catch { }
//            try { _client?.Dispose(); } catch { }

//            _stream = null;
//            _client = null;
//            isConnected = false;
//        }

//        private async Task<int> ReadWithTimeoutAsync(Stream stream, byte[] buffer, int offset, int count, int timeoutMs, CancellationToken cancellationToken)
//        {
//            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
//            cts.CancelAfter(timeoutMs);
//            try
//            {
//                return await stream.ReadAsync(buffer.AsMemory(offset, count), cts.Token);
//            }
//            catch (OperationCanceledException)
//            {
//                throw new IOException("Veri okuma zaman aşımına uğradı.");
//            }
//        }

//        private async Task DiscardUntilNewlineAsync(NetworkStream stream, CancellationToken token)
//        {
//            var discardBuffer = new byte[1];
//            const int maxBytesToDiscard = 2048;
//            int bytesDiscarded = 0;

//            while (!token.IsCancellationRequested && bytesDiscarded < maxBytesToDiscard)
//            {
//                int read = await ReadWithTimeoutAsync(stream, discardBuffer, 0, 1, 10000000, token);
//                if (read == 0 || discardBuffer[0] == (byte)'\n')
//                {
//                    Console.WriteLine($"Discard: \\n bulundu. {bytesDiscarded} byte atlandı.");
//                    return;
//                }
//                bytesDiscarded++;
//            }

//            if (bytesDiscarded >= maxBytesToDiscard)
//                Console.WriteLine("Uyarı: \\n bulunamadı, buffer temizleme sınırı aşıldı.");
//        }

//        private async Task HandleServerAsync(TcpClient client, NetworkStream stream, CancellationToken stoppingToken)
//        {
//            try
//            {
//                var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
//                const int LENGTH_HEADER_SIZE = 4;
//                const int MAX_ALLOWED_LENGTH = 256 * 1024; // 256 KB sınır
//                var lengthBuffer = new byte[LENGTH_HEADER_SIZE];

//                while (!stoppingToken.IsCancellationRequested && client.Connected)
//                {
//                    int totalRead = 0;
//                    while (totalRead < LENGTH_HEADER_SIZE)
//                    {
//                        int bytesRead = await ReadWithTimeoutAsync(stream, lengthBuffer, totalRead, LENGTH_HEADER_SIZE - totalRead, 700, stoppingToken);
//                        if (bytesRead == 0)
//                            throw new IOException("Uzunluk alınamadı.");
//                        totalRead += bytesRead;
//                    }

//                    int messageLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
//                    if (messageLength <= 0 || messageLength > MAX_ALLOWED_LENGTH)
//                        throw new InvalidDataException($"Geçersiz uzunluk: {messageLength}");

//                    using var ms = new MemoryStream(capacity: messageLength);
//                    int remaining = messageLength;
//                    while (remaining > 0)
//                    {
//                        var buffer = new byte[Math.Min(2048, remaining)];
//                        int bytesRead = await ReadWithTimeoutAsync(stream, buffer, 0, buffer.Length, 700, stoppingToken);
//                        if (bytesRead == 0)
//                            throw new IOException("Mesaj tamamlanamadı.");

//                        await ms.WriteAsync(buffer.AsMemory(0, bytesRead), stoppingToken);
//                        remaining -= bytesRead;
//                    }

//                    var newlineBuffer = new byte[1];
//                    int readNewline = await ReadWithTimeoutAsync(stream, newlineBuffer, 0, 1, 700, stoppingToken);
//                    if (readNewline == 0 || newlineBuffer[0] != (byte)'\n')
//                    {
//                        Console.WriteLine("Uyarı: \\n eksik veya hatalı. Buffer temizleniyor...");
//                        await DiscardUntilNewlineAsync(stream, stoppingToken);
//                        continue;
//                    }

//                    var jsonString = Encoding.UTF8.GetString(ms.ToArray());
//                    if (string.IsNullOrWhiteSpace(jsonString))
//                        continue;

//                    var options = new JsonSerializerOptions
//                    {
//                        PropertyNameCaseInsensitive = true,
//                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
//                    };

//                    var trainData = JsonSerializer.Deserialize<JsonDocumentFormatUDP.TrainData>(jsonString, options);

//                    if (trainData == null || HasNullProperty(trainData))
//                    {
//                        Console.WriteLine("Uyarı: Null alanlı veya hatalı JSON. Mesaj atlanıyor.");
//                        continue;
//                    }

//                    var command = new ReadDataFromTCMSWithTCPCommand
//                    {
//                        DataBytes = Encoding.UTF8.GetBytes(jsonString),
//                        RemoteEndPoint = remoteEndPoint
//                    };

//                    var result = await mediator.Send(command, stoppingToken);
//                    if (result is not null)
//                    {
//                        await mediator.Send(new SendCoupledDataToCoupleExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                        await mediator.Send(new SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand { trainData = result }, stoppingToken);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Veri işleme hatası: {ex.Message}");
//                CleanupConnection();
//            }
//        }

//        private bool HasNullProperty(object obj)
//        {
//            if (obj == null) return true;
//            var type = obj.GetType();
//            foreach (var property in type.GetProperties())
//            {
//                if (property.Name == nameof(JsonDocumentFormatUDP.TrainData.CouplingTrainsId)) continue;
//                var value = property.GetValue(obj);
//                if (value == null) return true;
//                if (!property.PropertyType.IsPrimitive && property.PropertyType != typeof(string))
//                    if (HasNullProperty(value)) return true;
//            }
//            return false;
//        }

//        private string GetLocalIPAddress()
//        {
//            var host = Dns.GetHostEntry(Dns.GetHostName());
//            foreach (var ip in host.AddressList)
//                if (ip.AddressFamily == AddressFamily.InterNetwork)
//                    return ip.ToString();
//            return "127.0.0.1";
//        }

//        private async Task HandleWriteAsync(NetworkStream stream, CancellationToken cancellationToken)
//        {
//            while (!cancellationToken.IsCancellationRequested)
//            {
//                var now = DateTime.UtcNow;

//                var outgoingData = new
//                {
//                    TimeStamp = now.ToString("yyyy-MM-ddTHH:mm:ss"),
//                    Date = now.ToString("yyyy-MM-dd"),
//                    Time = now.ToString("HH:mm:ss"),
//                    IP = GetLocalIPAddress(),
//                    Heartbeat = 1,
//                    YBS_Announcement_State = true,
//                    YBS_Intercom_State = true,
//                    YBS_Intercom_1 = false,
//                    YBS_Intercom_2 = false,
//                    YBS_Intercom_3 = false,
//                    YBS_Intercom_4 = false,
//                    YBS_Intercom_5 = false,
//                    YBS_Intercom_6 = false,
//                    YBS_Intercom_7 = false,
//                    YBS_Intercom_8 = false
//                };

//                var json = JsonSerializer.Serialize(outgoingData);
//                var bytes = Encoding.UTF8.GetBytes(json);

//                try
//                {
//                    await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
//                    Console.WriteLine("Gönderildi: " + json);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine("Gönderim hatası: " + ex.Message);
//                    break;
//                }

//                await Task.Delay(500, cancellationToken);
//            }
//        }
//    }
//}


using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Tram34TCMSInterface.Application.Features.ReadDataFromTCMSWithTCP;
using Tram34TCMSInterface.Application.Features.SendCoupledDataToCoupleExchangeFromTCMS;
using Tram34TCMSInterface.Application.Features.SendTakoMeterPulseDataToTakoReadExchangeFromTCMS;
using Tram34TCMSInterface.Domain.Models;
using System.Text.Json.Serialization;

namespace Tram34TCMSInterface.Infrastructure.BackgroundServices
{
    public class ReadDataFromTCMSWithTCPBackgroundService : BackgroundService
    {
        private readonly IMediator mediator;
        private readonly IConfiguration configuration;
        private TcpClient _client;
        private NetworkStream _stream;
        private bool isConnected = false;

        public ReadDataFromTCMSWithTCPBackgroundService(IMediator mediator, IConfiguration configuration)
        {
            this.mediator = mediator;
            this.configuration = configuration;
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

                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        socket.Bind(new IPEndPoint(localIPAddress, localPort));

                        _client = new TcpClient { Client = socket };
                        await _client.ConnectAsync(remoteIPAddress, remotePort, stoppingToken);
                        _stream = _client.GetStream();
                        isConnected = true;

                        Console.WriteLine($"Bağlandı: {localIPAddress}:{localPort} → {remoteIPAddress}:{remotePort}");

                        _ = Task.Run(() => HandleWriteAsync(_stream, stoppingToken), stoppingToken);
                    }

                    await HandleServerAsync(_client, _stream, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Bağlantı hatası: {ex.Message}");
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

        private async Task<int> ReadWithTimeoutAsync(Stream stream, byte[] buffer, int offset, int count, int timeoutMs, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);
            try
            {
                return await stream.ReadAsync(buffer.AsMemory(offset, count), cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new IOException("Veri okuma zaman aşımına uğradı.");
            }
        }

        private async Task DiscardUntilNewlineAsync(NetworkStream stream, CancellationToken token)
        {
            var discardBuffer = new byte[1];
            const int maxBytesToDiscard = 2048;
            int bytesDiscarded = 0;

            while (!token.IsCancellationRequested && bytesDiscarded < maxBytesToDiscard)
            {
                int read = await ReadWithTimeoutAsync(stream, discardBuffer, 0, 1, 500, token);
                if (read == 0 || discardBuffer[0] == (byte)'\n')
                {
                    Console.WriteLine($"Discard: \\n bulundu. {bytesDiscarded} byte atlandı.");
                    return;
                }
                bytesDiscarded++;
            }

            if (bytesDiscarded >= maxBytesToDiscard)
                Console.WriteLine("Uyarı: \\n bulunamadı, buffer temizleme sınırı aşıldı.");
        }

        private async Task HandleServerAsync(TcpClient client, NetworkStream stream, CancellationToken stoppingToken)
        {
            try
            {
                var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                const int LENGTH_HEADER_SIZE = 4;
                var lengthBuffer = new byte[LENGTH_HEADER_SIZE];

                while (!stoppingToken.IsCancellationRequested && client.Connected)
                {
                    int totalRead = 0;
                    while (totalRead < LENGTH_HEADER_SIZE)
                    {
                        int bytesRead = await ReadWithTimeoutAsync(stream, lengthBuffer, totalRead, LENGTH_HEADER_SIZE - totalRead, 700, stoppingToken);
                        if (bytesRead == 0)
                        {
                            Console.WriteLine("Uyarı: uzunluk alınamadı.");
                            continue;
                        }
                        totalRead += bytesRead;
                    }

                    int messageLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
                    if (messageLength <= 0 || messageLength > 1024 * 1024)
                    {
                        Console.WriteLine($"Uyarı: Geçersiz uzunluk: {messageLength}");
                        await DiscardUntilNewlineAsync(stream, stoppingToken);
                        continue;
                    }

                    using var ms = new MemoryStream();
                    int remaining = messageLength;
                    bool incomplete = false;

                    while (remaining > 0)
                    {
                        var buffer = new byte[Math.Min(2048, remaining)];
                        int bytesRead;
                        try
                        {
                            bytesRead = await ReadWithTimeoutAsync(stream, buffer, 0, buffer.Length, 700, stoppingToken);
                        }
                        catch
                        {
                            Console.WriteLine("Uyarı: Mesaj eksik geldi. Buffer atlanıyor...");
                            await DiscardUntilNewlineAsync(stream, stoppingToken);
                            incomplete = true;
                            break;
                        }

                        if (bytesRead == 0)
                        {
                            Console.WriteLine("Uyarı: Mesaj erken kesildi. Buffer atlanıyor...");
                            await DiscardUntilNewlineAsync(stream, stoppingToken);
                            incomplete = true;
                            break;
                        }

                        await ms.WriteAsync(buffer.AsMemory(0, bytesRead), stoppingToken);
                        remaining -= bytesRead;
                    }

                    if (incomplete)
                        continue;

                    var newlineBuffer = new byte[1];
                    int readNewline = await ReadWithTimeoutAsync(stream, newlineBuffer, 0, 1, 700, stoppingToken);
                    if (readNewline == 0 || newlineBuffer[0] != (byte)'\n')
                    {
                        Console.WriteLine("Uyarı: \\n eksik veya hatalı. Buffer temizleniyor...");
                        await DiscardUntilNewlineAsync(stream, stoppingToken);
                        continue;
                    }

                    var jsonString = Encoding.UTF8.GetString(ms.ToArray());
                    if (string.IsNullOrWhiteSpace(jsonString))
                        continue;

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    var trainData = JsonSerializer.Deserialize<JsonDocumentFormatUDP.TrainData>(jsonString, options);

                    if (trainData == null || HasNullProperty(trainData))
                    {
                        Console.WriteLine("Uyarı: Null alanlı veya hatalı JSON. Mesaj atlanıyor.");
                        continue;
                    }

                    var command = new ReadDataFromTCMSWithTCPCommand
                    {
                        DataBytes = Encoding.UTF8.GetBytes(jsonString),
                        RemoteEndPoint = remoteEndPoint
                    };

                    var result = await mediator.Send(command, stoppingToken);
                    if (result is not null)
                    {
                        await mediator.Send(new SendCoupledDataToCoupleExchangeFromTCMSCommand { trainData = result }, stoppingToken);
                        await mediator.Send(new SendTakoMeterPulseDataToTakoReadExchangeFromTCMSCommand { trainData = result }, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Veri işleme hatası: {ex.Message}");
                CleanupConnection();
            }
        }

        private bool HasNullProperty(object obj)
        {
            if (obj == null) return true;
            var type = obj.GetType();
            foreach (var property in type.GetProperties())
            {
                if (property.Name == nameof(JsonDocumentFormatUDP.TrainData.CouplingTrainsId)) continue;
                var value = property.GetValue(obj);
                if (value == null) return true;
                if (!property.PropertyType.IsPrimitive && property.PropertyType != typeof(string))
                    if (HasNullProperty(value)) return true;
            }
            return false;
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            return "127.0.0.1";
        }

        private async Task HandleWriteAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                var outgoingData = new
                {
                    TimeStamp = now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Date = now.ToString("yyyy-MM-dd"),
                    Time = now.ToString("HH:mm:ss"),
                    IP = GetLocalIPAddress(),
                    Heartbeat = 1,
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

                try
                {
                    await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                    Console.WriteLine("Gönderildi: " + json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Gönderim hatası: " + ex.Message);
                    break;
                }

                await Task.Delay(500, cancellationToken);
            }
        }
    }
}




