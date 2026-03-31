using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Tram34TCMSInterface.Application.Abstractions.LogService;
using Tram34TCMSInterface.Domain.Log;


namespace Tram34TCMSInterface.Infrastructure.RabbitMQ
{
    public class RabbitService : IRabbitService
    {
        private readonly ILogFactory _logFactory;
        private readonly ILogService _logService;
        private readonly IConfiguration configuration;
        public RabbitService(ILogFactory logFactory, ILogService logService, IConfiguration configuration)
        {
            _logFactory = logFactory;
            _logService = logService;
            this.configuration = configuration;
        }

        private readonly ConcurrentDictionary<string, IConnection> _connections = new();
        private readonly ConcurrentDictionary<string, (List<IChannelConfiguration> Active, List<IChannelConfiguration> Lost)> _channelConfigurations = new();

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _hostSemaphores = new();
        private readonly List<UnsetMessages> unsetMessages = new();

        private async Task<IConnection> CreateConnectionAsync(string host, CancellationToken cancellationToken = default)
        {
            if (_connections.TryGetValue(host, out IConnection existingConnection) && existingConnection.IsOpen)
                return existingConnection;

            Console.WriteLine($"{host} bağlantısı kapalı! Yeni bağlantı oluşturuluyor...");
            ConnectionFactory _factory = new()
            {
                HostName = host,
                AutomaticRecoveryEnabled = false,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(1),
                RequestedHeartbeat = TimeSpan.FromMinutes(1)
            };

            using (var newConnection = await _factory.CreateConnectionAsync(cancellationToken))
            {
                _factory.AutomaticRecoveryEnabled = true;
                _connections[host] = await _factory.CreateConnectionAsync(cancellationToken);
            };

            _connections[host].ConnectionShutdownAsync += async (sender, @event) => await ConnectionShutdownAsync(sender, @event, host);
            _connections[host].ConnectionRecoveryErrorAsync += async (sender, args) => await ConnectionRecoveryErrorAsync(sender, args, host);
            _connections[host].RecoverySucceededAsync += async (sender, args) => await RecoverySucceededAsync(sender, args, host);
            return _connections[host];
        }

        private async Task RecoverySucceededAsync(object sender, AsyncEventArgs @event, string host)
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


            _logService.SendLogAsync<EventLog>(
                _logFactory.CreateEventLog($"[RabbitMQ][{host}] RabbitMQ {host} bağlantısı yeniden oluşturuldu.", configuration["Log:TCMSSource"], "RabbitMQ"));

            await Task.CompletedTask;
        }


        private async Task ConnectionRecoveryErrorAsync(object sender, ConnectionRecoveryErrorEventArgs @event, string host)
        {
            Console.WriteLine($"{host} için RabbitMQ bağlantısı yeniden bağlanıyor... {@event.Exception.Message}");


            _logService.SendLogAsync(_logFactory.CreateErrorLog($"[ConnectionRecoveryError][RabbitMQ][{host}] RabbitMQ'da {host} için yeniden bağlanmayı deniyor.", configuration["Log:TCMSSource"], "Software"));

            await Task.CompletedTask;
        }

        private async Task ConnectionShutdownAsync(object sender, ShutdownEventArgs @event, string host)
        {
            Console.WriteLine($"{host} için RabbitMQ bağlantısı kesildi... {@event.ReplyText}");


            _logService.SendLogAsync(_logFactory.CreateErrorLog($"[ConnectionShutdown][RabbitMQ][{host}] RabbitMQ'da {host} için bağlantı kesildi.", configuration["Log:TCMSSource"], "Software"));

            await Task.CompletedTask;
        }

        private async Task<IConnection> GetConnectionAsync(string host, CancellationToken cancellationToken = default)
        {
            if (_connections.TryGetValue(host, out IConnection existingConnection) && existingConnection.IsOpen)
                return existingConnection;

            if (existingConnection?.CloseReason?.ReplyCode is not null)
                throw new Exception("Yeniden bağlantı bekleniyor.");

            var semaphore = _hostSemaphores.GetOrAdd(host, _ => new SemaphoreSlim(1, 1));

            var connectionStatus = _connections.TryGetValue(host, out IConnection hostConnection);
            int attempt = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                attempt++;
                await semaphore.WaitAsync();
                try
                {
                    if (host != RabbitMQConstant.RabbitMQHost)
                    {

                        Console.WriteLine($"Tren kuplajında [{host}] eksiktir.");
                        return null;

                    }
                    var connection = await CreateConnectionAsync(host, cancellationToken);
                    if (connection != null && connection.IsOpen)
                        return connection;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{host} için ilk bağlantı hatası ({attempt}. deneme): {ex.Message}");

                    _logService.SendLogAsync(_logFactory.CreateErrorLog($"[BrokerUnreachableException][RabbitMQ][{host}] RabbitMQ {host}'a bağlanamadı.", configuration["Log:TCMSSource"], "Software"));

                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
                finally
                {
                    semaphore.Release();
                }
            }
            throw new OperationCanceledException("RabbitMQ bağlantısı sağlanamadı.");
        }

        private void AddMessage(UnsetMessages newMessage)
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

        private async Task ConsumeAsync<T>(IChannel channel, string queueName, ManagementEnum management, Func<T, Task> act)
        {
            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    byte[] body = ea.Body.ToArray();

                    string message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"RAW MESSAGE: {message}");
                    Console.WriteLine($"BODY LENGTH: {body.Length}");
                    T? data = JsonSerializer.Deserialize<T>(message);

                    if (data != null)
                        await act(data);

                    if (management == ManagementEnum.LastMessage && ea.DeliveryTag > 1)
                        await channel.BasicAckAsync(ea.DeliveryTag - 1, true);

                    if (management == ManagementEnum.Live || management == ManagementEnum.UnlostMessage)
                        await channel.BasicAckAsync(ea.DeliveryTag, false);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Consume error: " + ex.Message);
                    string host = _channelConfigurations
                    .Where(kvp => kvp.Value.Active.Any(c => c.Channel == channel) || kvp.Value.Lost.Any(c => c.Channel == channel))
                    .Select(kvp => kvp.Key)
                    .FirstOrDefault();


                    Console.WriteLine($"[JsonException][RabbitMQ][{host}] RabbitMQ {consumer.Channel.CurrentQueue} kuyruğuna yanlış türde veri geldi.");
                    _logService.SendLogAsync(_logFactory.CreateErrorLog($"[JsonException][RabbitMQ][{host}] RabbitMQ {consumer.Channel.CurrentQueue} kuyruğuna yanlış türde veri geldi.", configuration["Log:TCMSSource"], "Software"));

                }
            };

            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
        }

        private async Task BindDeclareAsync(IChannel channel, string exchangeName, string exchangeType, string queueName, string routingKey, ManagementEnum management)
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

        public async Task ConsumerAsync<T>(string host, string exchangeName, string exchangeType, string queueName, string routingKey, ManagementEnum management, Func<T, Task> act)
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

                //channel.ChannelShutdownAsync += async (sender, @event) => await ChannelShutdownAsync(sender, @event, host);
                await BindDeclareAsync(channel, exchangeName, exchangeType, queueName, routingKey, management);
                await ConsumeAsync<T>(channel, queueName, management, act);

                _channelConfigurations[host].Active.Add(new ChannelConfiguration<T>(host, exchangeName, exchangeType, queueName, routingKey, management, act, channel));


                Console.WriteLine($"[{host}] -> [{queueName}] Bağlandı");
                _logService.SendLogAsync<InformationLog>(_logFactory.CreateInformationLog($"[RabbitMQ][{host}]. RabbitMQ {host} -> {queueName} kuyruğuna bağlandı.", configuration["Log:TCMSSource"]));

            }
            catch (Exception ex)
            {

                _logService.SendLogAsync(_logFactory.CreateErrorLog($"[{ex.GetType().Name}][RabbitMQ][{host}] RabbitMQ {host} -> {queueName} kuyruğuna bağlanırken bir hata oluştu.", configuration["Log:TCMSSource"], "Software"));

                _channelConfigurations[host].Lost.Add(new ChannelConfiguration<T>(host, exchangeName, exchangeType, queueName, routingKey, management, act, channel));
                Console.WriteLine("Hata oluştu :" + ex.Message);
            }
        }

        public async Task<bool> PublishMessage(string host, string exchangeName, string exchangeType, string routingKey, object obj, ManagementEnum management, CancellationToken cancellationToken = default)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                IConnection connection = await GetConnectionAsync(host, linkedCts.Token);
                if (connection == null)
                    return false;
                using IChannel channel = await connection.CreateChannelAsync();
                await channel.ExchangeDeclareAsync(exchange: exchangeName, type: exchangeType, durable: true, autoDelete: false);
                var message = JsonSerializer.Serialize(obj);
                var body = Encoding.UTF8.GetBytes(message);
                var properties = new BasicProperties() { Persistent = true };
                await channel.BasicPublishAsync(exchange: exchangeName, routingKey: routingKey, mandatory: false, basicProperties: properties, body: body);
                Console.WriteLine($"[{host}] -> [{exchangeName}] mesaj gönderildi.");

                /*_logService.SendLogAsync<InformationLog>(_logFactory.CreateInformationLog($"[RabbitMQ][{host}]. RabbitMQ {host} -> {exchangeName} exchange'ine veri gönderildi.", "HMIController"));*/


                _logService.SendLogAsync<EventLog>(
                    _logFactory.CreateEventLog($"[RabbitMQ][{host}]. RabbitMQ {host} -> {exchangeName} exchange'ine veri gönderildi.", configuration["Log:TCMSSource"], "RabbitMQ"));


                return true;
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine("Publish işlemi 1 saniyeyi geçti ve iptal edildi.");

                _logService.SendLogAsync(_logFactory.CreateErrorLog($"[OperationCanceledException][RabbitMQ][{host}] RabbitMQ {host} -> {exchangeName} exchange'ine veri gönderilemedi.", configuration["Log:TCMSSource"], "Software"));

                return false;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"[{host}] -> [{exchangeName}] Mesaj gönderilirken hata oluştu: " + ex.Message);

                _logService.SendLogAsync(_logFactory.CreateErrorLog($"[{ex.GetType().Name}][RabbitMQ][{host}] RabbitMQ {host} -> {exchangeName} exchange'ine veri gönderilemedi.", configuration["Log:TCMSSource"], "Software"));

                if (management == ManagementEnum.LastMessage)
                {
                    AddMessage(new() { Host = host, ExchangeName = exchangeName, ExchangeType = exchangeType, RoutingKey = routingKey, Obj = obj, Management = management });
                }
                else if (management == ManagementEnum.UnlostMessage)
                {
                    unsetMessages.Add(new() { Host = host, ExchangeName = exchangeName, ExchangeType = exchangeType, RoutingKey = routingKey, Obj = obj, Management = management });
                }
                return false;
            }
        }
    }
}