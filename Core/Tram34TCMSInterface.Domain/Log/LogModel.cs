namespace Tram34TCMSInterface.Domain.Log
{
    public class BaseLogModel
    {
        public string MessageType { get; set; }
        public string MessageContent { get; set; }
        public string MessageSource { get; set; }
        public DateTime DateTime { get; set; } = DateTime.Now;

        public BaseLogModel(string messageType)
        {
            MessageType = messageType;
        }

        // Reset metodu, log nesnesindeki tüm değerleri sıfırlayacak
        public virtual void Reset()
        {
            MessageContent = null;
            MessageSource = null;
            DateTime = DateTime.Now;
        }
    }

    public class EventLog : BaseLogModel
    {
        public string SourceIP { get; set; }
        public string DestinationName { get; set; }
        public string DestinationIP { get; set; }

        public EventLog(string messageType = "Event") : base(messageType)
        {
            MessageType = messageType;
        }

        // Reset metodu, log nesnesindeki tüm değerleri sıfırlayacak
        public override void Reset()
        {
            base.Reset();  // Base sınıfın Reset metodunu çağırır
            SourceIP = null;
            DestinationName = null;
            DestinationIP = null;
        }
    }

    public class ErrorLog : BaseLogModel
    {
        public string MessageSourceType { get; set; }
        public string HardwareIP { get; set; }

        public ErrorLog(string messageType = "Error") : base(messageType)
        {
            MessageType = messageType;
        }

        // Reset metodu, log nesnesindeki tüm değerleri sıfırlayacak
        public override void Reset()
        {
            base.Reset();  // Base sınıfın Reset metodunu çağırır
            MessageSourceType = null;
            HardwareIP = null;
        }
    }

    public class InformationLog : BaseLogModel
    {
        public InformationLog(string messageType = "Information") : base(messageType)
        {
            MessageType = messageType;
        }

        // Reset metodu, log nesnesindeki tüm değerleri sıfırlayacak
        public override void Reset()
        {
            base.Reset();  // Base sınıfın Reset metodunu çağırır
        }
    }

    public class AlarmLog : BaseLogModel
    {
        public string MessageSourceType { get; set; }
        public string HardwareIP { get; set; }

        public AlarmLog(string messageType = "Alarm") : base(messageType)
        {
            MessageType = messageType;
        }

        // Reset metodu, log nesnesindeki tüm değerleri sıfırlayacak
        public override void Reset()
        {
            base.Reset();  // Base sınıfın Reset metodunu çağırır
            MessageSourceType = null;
            HardwareIP = null;
        }
    }

    public class WarningLog : BaseLogModel
    {
        public string MessageSourceType { get; set; }
        public string HardwareIP { get; set; }

        public WarningLog(string messageType = "Warning") : base(messageType)
        {
            MessageType = messageType;
        }

        // Reset metodu, log nesnesindeki tüm değerleri sıfırlayacak
        public override void Reset()
        {
            base.Reset();  // Base sınıfın Reset metodunu çağırır
            MessageSourceType = null;
            HardwareIP = null;
        }
    }

}
