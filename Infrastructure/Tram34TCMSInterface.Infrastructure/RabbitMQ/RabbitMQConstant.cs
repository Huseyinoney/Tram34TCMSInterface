namespace Tram34TCMSInterface.Infrastructure.RabbitMQ
{
    public class RabbitMQConstant
    {
        public const string RabbitMQHost = "100.10.107.20";
        public const string ExchangeType = "fanout";

        public const string TakoReadExchangeName = "TakoRead";
        public const string LedExchangeName = "LedPush";
        public const string LedAddName = "LedsFromClient";
        public const string CoupledTrainsExchangeName = "CoupledTrains";
    }
}