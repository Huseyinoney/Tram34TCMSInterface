namespace RabbitMQ.Shared
{
    public class RabbitMQConstants
    {
        public const string RabbitMQHost = "100.10.107.20";
        public const string ExchangeType = "fanout";

        public const string TableExchangeName = "Table";
        public const string DataExchangeName = "Data";

        public const string AAppExchangeName = "AApp";

        public const string BAppExchangeName = "BApp";

        public const string CAppExchangeName = "CApp";

        public const string TakoReadExchangeName = "TakoRead";
        public const string LedExchangeName = "LedPush";
        public const string LedAddName = "LedsFromClient";
        public const string CoupledTrainsExchangeName = "CoupledTrains";
    }
}
