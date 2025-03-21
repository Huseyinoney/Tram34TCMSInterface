using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Tram34TCMSInterface.Infrastructure.RabbitMQ
{
    public class ChannelConfiguration<T> : IChannelConfiguration
    {
        public string Host { get; }
        public string ExchangeName { get; }
        public string ExchangeType { get; }
        public string QueueName { get; }
        public string RoutingKey { get; }
        public ManagementEnum Management { get; }
        public Func<T, Task> Act { get; }
        public IChannel Channel { get; set; }

        public ChannelConfiguration(string host, string exchangeName, string exchangeType, string queueName, string routingKey, ManagementEnum management, Func<T, Task> act, IChannel channel)
        {
            Host = host;
            ExchangeName = exchangeName;
            ExchangeType = exchangeType;
            QueueName = queueName;
            RoutingKey = routingKey;
            Management = management;
            Act = act;
            Channel = channel;
        }
        public async Task ConsumeAsync(IChannel channel)
        {
            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);
                T? data = JsonSerializer.Deserialize<T>(message);
                await Act(data);

                if (Management == ManagementEnum.Live || Management == ManagementEnum.UnlostMessage)
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
            };
            await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer);
        }
    }
}
