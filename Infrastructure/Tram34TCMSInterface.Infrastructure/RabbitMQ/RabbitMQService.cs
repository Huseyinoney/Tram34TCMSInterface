using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text;

namespace Tram34TCMSInterface.Infrastructure.RabbitMQ
{
    public static class RabbitMQService
    {
        private static readonly ConcurrentDictionary<string, IConnection> _connections = new();
        private static readonly ConcurrentDictionary<string, (List<IChannelConfiguration> Active, List<IChannelConfiguration> Lost)> _channelConfigurations = new();

        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private static readonly List<UnsetMessages> unsetMessages = new();
        private static readonly ConnectionFactory _factory = new();

        private static async Task<IConnection> CreateConnectionAsync(string host)
        {

            if (_connections.TryGetValue(host, out IConnection existingConnection) && existingConnection.IsOpen)
                return existingConnection;

            Console.WriteLine($"{host} bağlantısı kapalı! Yeni bağlantı oluşturuluyor...");
            _factory.HostName = host;
            _factory.AutomaticRecoveryEnabled = false;
            _factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
            _factory.RequestedHeartbeat = TimeSpan.FromSeconds(1);

            using (var newConnection = await _factory.CreateConnectionAsync())
            {
                _factory.AutomaticRecoveryEnabled = true;
                _connections[host] = await _factory.CreateConnectionAsync();
            };

            _connections[host].ConnectionShutdownAsync += async (sender, @event) => await ConnectionShutdownAsync(sender, @event, host);
            _connections[host].ConnectionRecoveryErrorAsync += async (sender, args) => await ConnectionRecoveryErrorAsync(sender, args, host);
            _connections[host].RecoverySucceededAsync += async (sender, args) => await RecoverySucceededAsync(sender, args, host);
            return _connections[host];
        }

        private static async Task RecoverySucceededAsync(object sender, AsyncEventArgs @event, string host)
        {
            Console.WriteLine($"[{host}] için RabbitMQ bağlantısı yeniden oluşturuldu...");

            if (_channelConfigurations.TryGetValue(host, out var configurations))
            {
                foreach (var config in configurations.Lost)
                {
                    IChannel newChannel = config.Channel ?? await _connections[host].CreateChannelAsync();
                    await BindDeclareAsync(newChannel, config.ExchangeName, config.ExchangeType, config.QueueName, config.RoutingKey, config.Management);
                    await config.ConsumeAsync(newChannel);
                    configurations.Active.Add(config);
                    Console.WriteLine("Kayıp channel oluşturuldu.");
                }
                configurations.Lost.Clear();
            }

            foreach (var unsetMessage in unsetMessages.ToList())
            {
                if (unsetMessage.Host == host)
                {
                    await PublishMessage(unsetMessage.Host, unsetMessage.ExchangeName, unsetMessage.ExchangeType, unsetMessage.RoutingKey, unsetMessage.Obj, unsetMessage.Management);
                    unsetMessages.Remove(unsetMessage);
                }
            }

            await Task.CompletedTask;
        }

        private static async Task ConnectionRecoveryErrorAsync(object sender, ConnectionRecoveryErrorEventArgs @event, string host)
        {
            Console.WriteLine($"{host} için RabbitMQ bağlantısı yeniden bağlanıyor... {@event.Exception.Message}");
            await Task.CompletedTask;
        }

        private static async Task ConnectionShutdownAsync(object sender, ShutdownEventArgs @event, string host)
        {
            Console.WriteLine($"{host} için RabbitMQ bağlantısı kesildi... {@event.ReplyText}");
            await Task.CompletedTask;
        }

        private static async Task<IConnection> GetConnectionAsync(string host)
        {
            if (_connections.TryGetValue(host, out IConnection existingConnection) && existingConnection.IsOpen)
                return existingConnection;

            int attempt = 1;
            var connectionStatus = _connections.TryGetValue(host, out IConnection hostConnection);
            while (!connectionStatus)
            {
                await _semaphore.WaitAsync();
                try
                {
                    var connection = await CreateConnectionAsync(host);
                    if (connection != null && connection.IsOpen)
                        return connection;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" {host} için ilk bağlantı hatası ({attempt}. deneme): {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
                finally
                {
                    _semaphore.Release();
                }
                attempt++;
            }
            throw new Exception("RabbitMQ bağlantısı sağlanamadı.");
        }

        //public static async Task<bool> PublishMessage(string host, string exchangeName, string exchangeType, string routingKey, object obj, ManagementEnum management)
        //{
        //    try
        //    {
        //        IConnection connection = await GetConnectionAsync(host);
        //        using IChannel channel = await connection.CreateChannelAsync();
        //        await channel.ExchangeDeclareAsync(exchange: exchangeName, type: exchangeType, durable: true, autoDelete: false);
        //        var message = JsonSerializer.Serialize(obj);
        //        var body = Encoding.UTF8.GetBytes(message);
        //        var properties = new BasicProperties() { Persistent = true };
        //        await channel.BasicPublishAsync(exchange: exchangeName, routingKey: routingKey, mandatory: false, basicProperties: properties, body: body);
        //        //Console.WriteLine($"[{host}] -> [{exchangeName}] mesaj gönderildi.");
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {

        //        Console.WriteLine($"[{host}] -> [{exchangeName}] Mesaj gönderilirken hata oluştu: " + ex.Message);
        //        if (management == ManagementEnum.LastMessage)
        //        {
        //            AddMessage(new() { Host = host, ExchangeName = exchangeName, ExchangeType = exchangeType, RoutingKey = routingKey, Obj = obj });
        //        }
        //        else if (management == ManagementEnum.UnlostMessage)
        //        {
        //            unsetMessages.Add(new() { Host = host, ExchangeName = exchangeName, ExchangeType = exchangeType, RoutingKey = routingKey, Obj = obj });
        //        }
        //        return false;
        //    }
        //}

        public static async Task<bool> PublishMessage(string host, string exchangeName, string exchangeType, string routingKey, object obj, ManagementEnum management)
        {
            try
            {
                IConnection connection = await GetConnectionAsync(host);
                using IChannel channel = await connection.CreateChannelAsync();
                await channel.ExchangeDeclareAsync(exchange: exchangeName, type: exchangeType, durable: true, autoDelete: false);
                var message = JsonSerializer.Serialize(obj);
                var body = Encoding.UTF8.GetBytes(message);
                var properties = new BasicProperties()
                {
                    Persistent = true,
                    MessageId = Guid.NewGuid().ToString()
                };
                await channel.BasicPublishAsync(exchange: exchangeName, routingKey: routingKey, mandatory: false, basicProperties: properties, body: body);
                //Console.WriteLine($"[{host}] -> [{exchangeName}] mesaj gönderildi.");
                return true;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"[{host}] -> [{exchangeName}] Mesaj gönderilirken hata oluştu: " + ex.Message);
                if (management == ManagementEnum.LastMessage)
                {
                    AddMessage(new() { Host = host, ExchangeName = exchangeName, ExchangeType = exchangeType, RoutingKey = routingKey, Obj = obj });
                }
                else if (management == ManagementEnum.UnlostMessage)
                {
                    unsetMessages.Add(new() { Host = host, ExchangeName = exchangeName, ExchangeType = exchangeType, RoutingKey = routingKey, Obj = obj });
                }
                return false;
            }
        }




        private static void AddMessage(UnsetMessages newMessage)
        {
            // Aynı Host, ExchangeName, ExchangeType ve RoutingKey değerine sahip olanları sil
            unsetMessages.RemoveAll(m =>
                m.Host == newMessage.Host &&
                m.ExchangeName == newMessage.ExchangeName &&
                m.ExchangeType == newMessage.ExchangeType &&
                m.RoutingKey == newMessage.RoutingKey);
            // Yeni mesajı listeye ekle
            unsetMessages.Add(newMessage);
        }

        public static async Task ConsumerAsync<T>(string host, string exchangeName, string exchangeType, string queueName, string routingKey, ManagementEnum management, Func<T, Task> act)
        {
            IChannel channel = null;
            try
            {
                if (_channelConfigurations.TryGetValue(host, out var configurations) && (configurations.Active.Any(c => c.QueueName == queueName) || configurations.Lost.Any(c => c.QueueName == queueName)))
                {
                    Console.WriteLine($"[{host}] -> [{queueName}] için zaten bir kanal tanımlı.");
                    return;
                }
                if (!_channelConfigurations.ContainsKey(host))
                {
                    _channelConfigurations[host] = (new List<IChannelConfiguration>(), new List<IChannelConfiguration>());
                }

                IConnection connection = await GetConnectionAsync(host);
                channel = await connection.CreateChannelAsync();
                await BindDeclareAsync(channel, exchangeName, exchangeType, queueName, routingKey, management);
                await ConsumeAsync<T>(channel, queueName, management, act);

                _channelConfigurations[host].Active.Add(new ChannelConfiguration<T>(host, exchangeName, exchangeType, queueName, routingKey, management, act, channel));


                Console.WriteLine($"[{host}] -> [{queueName}] Bağlandı");
            }
            catch (Exception ex)
            {
                _channelConfigurations[host].Lost.Add(new ChannelConfiguration<T>(host, exchangeName, exchangeType, queueName, routingKey, management, act, channel));
                Console.WriteLine("Hata oluştu :" + ex.Message);
            }
        }

        private static async Task ConsumeAsync<T>(IChannel channel, string queueName, ManagementEnum management, Func<T, Task> act)
        {
            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);
                T? data = JsonSerializer.Deserialize<T>(message);
                await act(data);

                if (management == ManagementEnum.Live || management == ManagementEnum.UnlostMessage)
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
            };
            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
        }

        private static async Task BindDeclareAsync(IChannel channel, string exchangeName, string exchangeType, string queueName, string routingKey, ManagementEnum management)
        {
            var args = new Dictionary<string, object>();

            if (management == ManagementEnum.Live)
            {
                await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: true, arguments: args);
            }
            else if (management == ManagementEnum.LastMessage)
            {
                args["x-max-length"] = 1;
                args["x-overflow"] = "drop-head";
                await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
            }
            else//UNLOST
            {
                await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
            }
            await channel.ExchangeDeclareAsync(exchange: exchangeName, type: exchangeType, durable: true, autoDelete: false);
            await channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: routingKey);
        }

    }

}
