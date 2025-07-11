namespace Tram34TCMSInterface.Infrastructure.RabbitMQ
{
    public class RabbitMQConstant
    {
        public const string RabbitMQHost = "100.10.102.20";
        public const string ExchangeType = "fanout";

        //TCMS'in Tako verisini LogicManager'a veri gönderdiği kuyruk İsmi
        public const string TakoReadExchangeName = "TakoRead";

        //TCMS'in Kuplajdaki Trenin Bilgilerini Diğer Uygulamaların Alması İçin Attığı Kuyruk İsmi
        public const string CoupledTrainsExchangeName = "CoupledTrains";
    }
}