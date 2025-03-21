using RabbitMQ.Client;

namespace Tram34TCMSInterface.Infrastructure.RabbitMQ
{
    public interface IChannelConfiguration
    {
        string Host { get; }
        string ExchangeName { get; }
        string ExchangeType { get; }
        string QueueName { get; }
        string RoutingKey { get; }
        ManagementEnum Management { get; }
        IChannel Channel { get; set; }
        Task ConsumeAsync(IChannel channel);

    }
}
