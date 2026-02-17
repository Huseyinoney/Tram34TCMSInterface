using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tram34TCMSInterface.Application.Common
{
    public interface ITrainContext
    {
        public string? TrainId { get; set; }
        public string? TrainIP { get; set; }
    }
}
