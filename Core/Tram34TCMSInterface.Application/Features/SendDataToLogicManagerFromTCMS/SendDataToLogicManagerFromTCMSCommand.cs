using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tram34TCMSInterface.Application.Features.SendDataToLogicManagerFromTCMS
{
    public class SendDataToLogicManagerFromTCMSCommand : IRequest<bool>
    {
        public Domain.Models.JsonDocumentFormatUDP.TrainData trainData { get; set; }
    }
}