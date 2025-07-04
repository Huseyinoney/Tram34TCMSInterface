using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static Tram34TCMSInterface.Domain.Models.JsonDocumentFormatUDP;

namespace Tram34TCMSInterface.Application.Features.ReadDataFromTCMSWithTCP
{
    public class ReadDataFromTCMSWithTCPCommand : IRequest<TrainData>
    {
        public byte[] DataBytes;
        public IPEndPoint RemoteEndPoint;
    }
}
