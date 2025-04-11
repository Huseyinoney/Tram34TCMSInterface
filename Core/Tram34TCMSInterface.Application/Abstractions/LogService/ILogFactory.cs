using Tram34TCMSInterface.Domain.Log;

namespace Tram34TCMSInterface.Application.Abstractions.LogService
{
    public interface ILogFactory
    {
        EventLog CreateEventLog(string messageContent, string messageSource, string sourceIP, string destinationName, string destinationIP);
        ErrorLog CreateErrorLog(string messageContent, string messageSource, string errorType, string hardwareIP);
        InformationLog CreateInformationLog(string messageContent, string messageSource);
        AlarmLog CreateAlarmLog(string messageContent, string messageSource, string alarmType, string hardwareIP);
        WarningLog CreateWarningLog(string messageContent, string messageSource, string warningType, string hardwareIP);
    }
}