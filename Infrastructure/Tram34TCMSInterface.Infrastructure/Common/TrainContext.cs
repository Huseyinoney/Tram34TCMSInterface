using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tram34TCMSInterface.Application.Common;

namespace Tram34TCMSInterface.Infrastructure.Common
{
    public class TrainContext : ITrainContext
    {
        public string? TrainId { get; set; }
        public string? TrainIP { get; set; }
    }
}
