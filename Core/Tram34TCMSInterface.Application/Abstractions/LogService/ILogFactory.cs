using Tram34TCMSInterface.Domain.Log;

namespace Tram34TCMSInterface.Application.Abstractions.LogService
{
    public interface ILogFactory
    {
        EventLog CreateEventLog(string messageContent, string messageSource, string destinationName);
        ErrorLog CreateErrorLog(string messageContent, string messageSource, string messageSourceType);
        InformationLog CreateInformationLog(string messageContent, string messageSource);
        AlarmLog CreateAlarmLog(string messageContent, string messageSource, string messageSourceType);
        WarningLog CreateWarningLog(string messageContent, string messageSource, string messageSourceType);
    }
}