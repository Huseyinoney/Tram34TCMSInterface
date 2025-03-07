using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tram34TCMSInterface.Application.Features.ReadDataFromTCMS
{
    public class ReadDataFromTCMSCommand : IRequest<JsonDocument>
    {
        public string IpAddress { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
    }
}