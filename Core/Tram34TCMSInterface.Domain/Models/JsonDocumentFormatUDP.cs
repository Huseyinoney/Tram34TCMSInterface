using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tram34TCMSInterface.Domain.Models
{
    public class JsonDocumentFormatUDP
    {
        public class TrainData
        {
            [JsonPropertyName("timeStamp")]
            public string TimeStamp { get; set; }

            [JsonPropertyName("masterTrainId")]
            public int MasterTrainId { get; set; }

            [JsonPropertyName("trainSpeed")]
            public double TrainSpeed { get; set; }

            [JsonPropertyName("zeroSpeed")]
            public bool ZeroSpeed { get; set; }

            [JsonPropertyName("tachoMeterPulse")]
            public bool TachoMeterPulse { get; set; }

            [JsonPropertyName("date")]
            public string Date { get; set; }

            [JsonPropertyName("time")]
            public string Time { get; set; }

            [JsonPropertyName("couplingTrainsId")]
            public CouplingTrainsId CouplingTrainsId { get; set; }

            [JsonPropertyName("train")]
            public Train TRAIN { get; set; }
        }

        public class CouplingTrainsId
        {
            [JsonPropertyName("couplingTrainsIdXX1")]
            public int CouplingTrainsIdXX1 { get; set; }

            [JsonPropertyName("couplingTrainsIdXX2")]
            public int? CouplingTrainsIdXX2 { get; set; }

            [JsonPropertyName("couplingTrainsIdXX3")]
            public int? CouplingTrainsIdXX3 { get; set; }

            [JsonPropertyName("couplingTrainsIdXXX")]
            public int? CouplingTrainsIdXXX { get; set; }
        }

        public class Train
        {
            [JsonPropertyName("id")]
            public dynamic ID { get; set; }

            [JsonPropertyName("ip")]
            public string IP { get; set; }

            [JsonPropertyName("cab_A_Active")]
            public bool Cab_A_Active { get; set; }

            [JsonPropertyName("cab_B_Active")]
            public bool Cab_B_Active { get; set; }

            [JsonPropertyName("cab_A_KeyStatus")]
            public bool Cab_A_KeyStatus { get; set; }

            [JsonPropertyName("cab_B_KeyStatus")]
            public bool Cab_B_KeyStatus { get; set; }

            [JsonPropertyName("trainCoupledOrder")]
            public int TrainCoupledOrder { get; set; }

            [JsonPropertyName("isTrainCoupled")]
            public bool IsTrainCoupled { get; set; }

            [JsonPropertyName("allDoorOpen")]
            public bool AllDoorOpen { get; set; }

            [JsonPropertyName("allDoorClose")]
            public bool AllDoorClose { get; set; }

            [JsonPropertyName("allDoorReleased")]
            public bool AllDoorReleased { get; set; }

            [JsonPropertyName("allLeftDoorOpen")]
            public bool AllLeftDoorOpen { get; set; }

            [JsonPropertyName("allRightDoorOpen")]
            public bool AllRightDoorOpen { get; set; }

            [JsonPropertyName("allLeftDoorClose")]
            public bool AllLeftDoorClose { get; set; }

            [JsonPropertyName("allRightDoorClose")]
            public bool AllRightDoorClose { get; set; }

            [JsonPropertyName("allLeftDoorReleased")]
            public bool AllLeftDoorReleased { get; set; }

            [JsonPropertyName("allRightDoorReleased")]
            public bool AllRightDoorReleased { get; set; }
        }

    }
}
