using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tram34TCMSInterface.Domain.Log;

namespace Tram34TCMSInterface.Application.Abstractions.MongoDB
{
    public interface IMongoDBRepository
    {
        public Task<TrainConfiguration?> GetTrainConfigurationFromMongoDBAsync(string trainId);
    }
}
