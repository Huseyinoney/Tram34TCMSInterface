using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Tram34TCMSInterface.Domain.Log
{
    public class TrainConfiguration
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? TrainId { get; set; }
        public List<Hardware>? Hardware { get; set; }
        public List<Software>? Software { get; set; }

    }
}
