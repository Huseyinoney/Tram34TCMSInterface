using MongoDB.Driver;
using Tram34TCMSInterface.Application.Abstractions.MongoDB;
using Tram34TCMSInterface.Domain.Log;
using Tram34TCMSInterface.Persistence.MongoDBContext;

namespace Tram34TCMSInterface.Infrastructure.Services
{
    public class MongoDBRepository : IMongoDBRepository
    {
        private readonly DBContextLog dbContextLog;
        public MongoDBRepository(DBContextLog dbContextLog)
        {
            this.dbContextLog = dbContextLog;
        }

        public async Task<TrainConfiguration?> GetTrainConfigurationFromMongoDBAsync(string trainId)
        {
            try
            {
                var filter = Builders<TrainConfiguration>.Filter.Eq(x => x.TrainId, trainId);
                return await dbContextLog.Log.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}