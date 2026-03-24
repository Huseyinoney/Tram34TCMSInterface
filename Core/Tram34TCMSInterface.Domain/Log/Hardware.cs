using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tram34TCMSInterface.Domain.Log
{
    public class Hardware
    {
        public string? Name { get; set; }
        public string? ip { get; set; }

        [BsonElement("Com Port")]
        public string? ComPort { get; set; }
    }

}
