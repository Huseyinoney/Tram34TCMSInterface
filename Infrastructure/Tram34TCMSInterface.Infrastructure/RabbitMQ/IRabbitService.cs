

namespace Tram34TCMSInterface.Infrastructure.RabbitMQ
{
    public interface IRabbitService
    {
        Task<bool> PublishMessage(string host, string exchangeName, string exchangeType, string routingKey, object obj, ManagementEnum management, CancellationToken cancellationToken = default);
        Task ConsumerAsync<T>(string host, string exchangeName, string exchangeType, string queueName, string routingKey, ManagementEnum management, Func<T, Task> act);
        
    }
}
