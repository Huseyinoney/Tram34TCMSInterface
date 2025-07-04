using MediatR;
using static Tram34TCMSInterface.Domain.Models.JsonDocumentFormatUDP;

namespace Tram34TCMSInterface.Application.Features.ReadDataFromTCMS
{
    public class ReadDataFromTCMSCommand : IRequest<TrainData>
    {
        //public TrainData JsonDocumentFormatUDP { get; set; }
    }
}