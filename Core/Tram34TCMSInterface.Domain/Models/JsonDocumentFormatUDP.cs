using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tram34TCMSInterface.Domain.Models
{
    public class JsonDocumentFormatUDP
    {
        public class CouplingTrain
        {
            [JsonPropertyName("couplingTrainsIds")]
            public List<int> CouplingTrainsIds { get; set; }

            public CouplingTrain()
            {
                CouplingTrainsIds = new List<int>();
            }

            public void ValidateCouplingTrainsIds()
            {
                if (CouplingTrainsIds.Count > 4)
                {
                    throw new InvalidOperationException("CouplingTrainsIds listesi en fazla 4 öğe içerebilir.");
                }
            }
        }

        public class Train
        {
            [JsonPropertyName("id")]
            public int ID { get; set; }

            [JsonPropertyName("ip")]
            public string IP { get; set; }

            [JsonPropertyName("cabAActive")]
            public bool CabAActive { get; set; }

            [JsonPropertyName("cabBActive")]
            public bool CabBActive { get; set; }

            [JsonPropertyName("cabineAKeyStatus")]
            public bool CabineAKeyStatus { get; set; }

            [JsonPropertyName("cabineBKeyStatus")]
            public bool CabineBKeyStatus { get; set; }

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

        public class TrainData
        {
            [JsonPropertyName("timeStamp")]
            public string TimeStamp { get; set; }

            [JsonPropertyName("masterTrainId")]
            public int MasterTrainId { get; set; }

            [JsonPropertyName("trainSpeed")]
            public int TrainSpeed { get; set; }

            [JsonPropertyName("zeroSpeed")]
            public bool ZeroSpeed { get; set; }

            [JsonPropertyName("tachoMeterPulse")]
            public bool TachoMeterPulse { get; set; }

            [JsonPropertyName("date")]
            public string Date { get; set; }

            [JsonPropertyName("time")]
            public string Time { get; set; }

            [JsonPropertyName("couplingTrainsIds")]
            public List<CouplingTrain> CouplingTrainsIds { get; set; }

            [JsonPropertyName("train")]
            public List<Train> TRAIN { get; set; }
        }
    }
}
