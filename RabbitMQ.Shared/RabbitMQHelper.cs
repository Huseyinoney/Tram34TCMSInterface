using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

public static class RabbitMQHelper
{
    private static readonly object _lock = new();
    private static IConnection _connection;
    private static IModel _channel;
    private static bool _isConnected = false;
    private static ConnectionFactory _factory;
    private static HashSet<string> declaredConsume = new HashSet<string>();

    private static void EnsureConnection(string host)
    {
        lock (_lock)
        {
            while (true) // Bağlantıyı sürekli dene
            {
                try
                {
                    // Bağlantıyı kontrol et, zaten açıksa dön
                    if (_connection != null && _connection.IsOpen)
                    {
                        return; // Bağlantı açıksa yeniden bağlanma
                    }

                    // Bağlantıyı sıfırlama
                    if (_connection != null)
                    {
                        _connection.ConnectionShutdown -= OnConnectionShutdown; // Event temizle
                    }

                    // Bağlantı Factory'i başlatıyoruz
                    _factory = new ConnectionFactory()
                    {
                        HostName = host,

                        NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                    };

                    // Yeni bağlantıyı oluştur
                    _connection = _factory.CreateConnection();
                    _connection.ConnectionShutdown += OnConnectionShutdown; // Bağlantı kapandığında yeniden bağlan

                    // Yeni channel oluşturulmaz, sadece bağlantıyı kontrol ederiz
                    if (_channel != null)
                    {
                        _channel.Dispose(); // Eski channel'ı dispose etme
                    }

                    _channel = _connection.CreateModel();
                    _isConnected = true;

                    Console.WriteLine("✅ RabbitMQ bağlantısı kuruldu.");
                    return;
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                    Console.WriteLine($"❌ Bağlantı hatası: {ex.Message} - 5 saniye sonra tekrar denenecek...");
                    Thread.Sleep(5000); // Bağlantı hatası durumunda 5 saniye bekle
                }
            }
        }
    }


    private static void OnConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        Console.WriteLine("⚠️ RabbitMQ bağlantısı kapandı! Yeniden bağlanıyor...");
        _isConnected = false;
        EnsureConnection(_factory.HostName); // Tekrar bağlan
    }

    public static void PublishMessage(string host, string exchangeName, string exchangeType, string routingKey, object obj)
    {
        Task.Run(() =>
        {
            EnsureConnection(host); // Bağlantıyı sağla (sürekli kontrol eder)

            try
            {
                // Bağlantı mevcutsa ve açık değilse yeniden kanal açma işlemi
                if (_channel == null || !_channel.IsOpen)
                {
                    // Kanalın kapanmış olma durumunda yeni bir kanal oluştur
                    _channel = _connection.CreateModel();
                    _channel.ExchangeDeclare(exchange: exchangeName, type: exchangeType, durable: true, autoDelete: false);
                    Console.WriteLine("✅ Yeni kanal oluşturuldu.");
                }

                var message = JsonSerializer.Serialize(obj);
                var body = Encoding.UTF8.GetBytes(message);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true; // Kuyruğa eklerken kalıcı yap

                _channel.BasicPublish(exchange: exchangeName, routingKey: routingKey, basicProperties: properties, body: body);

                //Console.WriteLine($"📤 Gönderildi: {message}");
            }
            catch (OperationInterruptedException ex)
            {
                Console.WriteLine($"❌ RabbitMQ bağlantısı kesildi. Tekrar bağlanılıyor... Hata: {ex.Message}");
                EnsureConnection(host); // Bağlantıyı tekrar sağla
                PublishMessage(host, exchangeName, exchangeType, routingKey, obj); // Tekrar mesajı yayınla
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Hata: {ex.Message}");
            }
        });
    }

    public static void ConsumeMessage<T>(string host, string exchangeName, string exchangeType, string queueName, string routingKey, Func<T, Task> act)
    {
        Task.Run(() =>
        {
            while (true)
            {
                EnsureConnection(host);

                try
                {
                    if (_channel == null || !_channel.IsOpen)
                    {
                        // Kanalın kapanmış olma durumunda yeni bir kanal oluştur
                        _channel = _connection.CreateModel();
                        Console.WriteLine("✅ Yeni kanal oluşturuldu.");
                    }
                    var args = new Dictionary<string, object>
                        {
                            { "x-max-length", 1 }, // Mesaj limit 1.
                            { "x-overflow", "drop-head" } // Eski mesajlar silinsin

                        };

                    _channel.ExchangeDeclare(exchange: exchangeName, type: exchangeType, durable: true, autoDelete: false);
                    _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
                    _channel.QueueBind(queueName, exchangeName, routingKey);

                    var consumer = new EventingBasicConsumer(_channel);
                    consumer.Received += async (model, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            var data = JsonSerializer.Deserialize<T>(message);

                            //Console.WriteLine($"📥 Alındı: {message}");

                            await act(data);

                            _channel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ İşleme hatası: {ex.Message}");
                        }
                    };
                    if (!declaredConsume.Contains(queueName))
                    {
                        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
                        Console.WriteLine($"🔄 {queueName} kuyruğu dinleniyor...\n");
                        declaredConsume.Add(queueName);

                    }
                    return;


                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Consumer başlatılamadı: {ex.Message} - 5 saniye sonra tekrar denenecek...");
                    Thread.Sleep(5000);
                }
            }
        });
    }

}
