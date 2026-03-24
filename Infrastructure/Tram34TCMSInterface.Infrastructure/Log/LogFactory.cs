using Tram34TCMSInterface.Application.Abstractions.Common;
using Tram34TCMSInterface.Application.Abstractions.LogService;
using Tram34TCMSInterface.Domain.Log;

public class LogFactory : ILogFactory
{
    private readonly ITrainContext trainContext;
    private readonly string DefaultIp = "LocalHost";
    public LogFactory(ITrainContext trainContext)
    {
        this.trainContext = trainContext;
    }

    private string Safe(string? value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value)
        ? defaultValue
        : value;
    }

    private string TrainIp => Safe(trainContext.TrainIP, DefaultIp);

    public EventLog CreateEventLog(string messageContent, string messageSource, string destinationName)
    {
        return new EventLog
        {
            MessageContent = messageContent,
            MessageSource = messageSource,
            SourceIP = TrainIp,
            DestinationName = destinationName,
            DestinationIP = TrainIp
        };
    }

    public ErrorLog CreateErrorLog(string messageContent, string messageSource, string messageSourceType)
    {
        return new ErrorLog
        {
            MessageContent = messageContent,
            MessageSource = messageSource,
            MessageSourceType = messageSourceType,
            HardwareIP = TrainIp
        };
    }

    public InformationLog CreateInformationLog(string messageContent, string messageSource)
    {
        return new InformationLog
        {
            MessageContent = messageContent,
            MessageSource = messageSource
        };
    }

    public AlarmLog CreateAlarmLog(string messageContent, string messageSource, string messageSourceType)
    {
        return new AlarmLog
        {
            MessageContent = messageContent,
            MessageSource = messageSource,
            MessageSourceType = messageSourceType,
            HardwareIP = TrainIp
        };
    }

    public WarningLog CreateWarningLog(string messageContent, string messageSource, string messageSourceType)
    {
        return new WarningLog
        {
            MessageContent = messageContent,
            MessageSource = messageSource,
            MessageSourceType = messageSourceType,
            HardwareIP = TrainIp
        };
    }
}
