using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tram34TCMSInterface.Domain.Models
{
    public class JsonDocumentFormatUDP
    {
        public class TrainData
        {
            [JsonPropertyName("TimeStamp")]
            public string TimeStamp { get; set; }

            [JsonPropertyName("MasterTrainId")]
            public string MasterTrainId { get; set; }

            [JsonPropertyName("TrainSpeed")]
            public double TrainSpeed { get; set; }

            [JsonPropertyName("ZeroSpeed")]
            public bool ZeroSpeed { get; set; }

            [JsonPropertyName("TachoMeterPulse")]
            public bool TachoMeterPulse { get; set; }

            [JsonPropertyName("Date")]
            public string Date { get; set; }

            [JsonPropertyName("Time")]
            public string Time { get; set; }

            [JsonPropertyName("CouplingTrainsId")]
            public CouplingTrainsId CouplingTrainsId { get; set; }

            [JsonPropertyName("TRAIN")]
            public Train TRAIN { get; set; }

            [JsonPropertyName("EMERGENCYSTATE")]
            public EmergencyState EMERGENCYSTATE { get; set; }
        }

        public class CouplingTrainsId
        {
            [JsonPropertyName("CouplingTrainsIdXX1")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string CouplingTrainsIdXX1 { get; set; }

            [JsonPropertyName("CouplingTrainsIdXX2")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string CouplingTrainsIdXX2 { get; set; }

            [JsonPropertyName("CouplingTrainsIdXX3")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string CouplingTrainsIdXX3 { get; set; }

            [JsonPropertyName("CouplingTrainsIdXX4")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string CouplingTrainsIdXX4 { get; set; }
        }

        public class Train
        {
            [JsonPropertyName("ID")]
            public string ID { get; set; }

            [JsonPropertyName("IP")]
            public string IP { get; set; }

            [JsonPropertyName("Cab_A_Active")]
            public bool Cab_A_Active { get; set; }

            [JsonPropertyName("Cab_B_Active")]
            public bool Cab_B_Active { get; set; }

            [JsonPropertyName("Cab_A_KeyStatus")]
            public bool Cab_A_KeyStatus { get; set; }

            [JsonPropertyName("Cab_B_KeyStatus")]
            public bool Cab_B_KeyStatus { get; set; }

            [JsonPropertyName("TrainCoupledOrder")]
            public int TrainCoupledOrder { get; set; }

            [JsonPropertyName("IsTrainCoupled")]
            public bool IsTrainCoupled { get; set; }

            [JsonPropertyName("AllDoorOpen")]
            public bool AllDoorOpen { get; set; }

            [JsonPropertyName("AllDoorClose")]
            public bool AllDoorClose { get; set; }

            [JsonPropertyName("AllDoorReleased")]
            public bool AllDoorReleased { get; set; }

            [JsonPropertyName("AllLeftDoorOpen")]
            public bool AllLeftDoorOpen { get; set; }

            [JsonPropertyName("AllRightDoorOpen")]
            public bool AllRightDoorOpen { get; set; }

            [JsonPropertyName("AllLeftDoorClose")]
            public bool AllLeftDoorClose { get; set; }

            [JsonPropertyName("AllRightDoorClose")]
            public bool AllRightDoorClose { get; set; }

            [JsonPropertyName("AllLeftDoorReleased")]
            public bool AllLeftDoorReleased { get; set; }

            [JsonPropertyName("AllRightDoorReleased")]
            public bool AllRightDoorReleased { get; set; }

            [JsonPropertyName("YBS_Intercom_1")]
            public bool YBS_Intercom_1 { get; set; }

            [JsonPropertyName("YBS_Intercom_2")]
            public bool YBS_Intercom_2 { get; set; }

            [JsonPropertyName("YBS_Intercom_3")]
            public bool YBS_Intercom_3 { get; set; }

            [JsonPropertyName("YBS_Intercom_4")]
            public bool YBS_Intercom_4 { get; set; }

            [JsonPropertyName("YBS_Intercom_5")]
            public bool YBS_Intercom_5 { get; set; }

            [JsonPropertyName("YBS_Intercom_6")]
            public bool YBS_Intercom_6 { get; set; }

            [JsonPropertyName("YBS_Intercom_7")]
            public bool YBS_Intercom_7 { get; set; }

            [JsonPropertyName("YBS_Intercom_8")]
            public bool YBS_Intercom_8 { get; set; }
        }

        public class EmergencyState
        {
            [JsonPropertyName("EmergencyStopHandle1")]
            public bool EmergencyStopHandle1 { get; set; }

            [JsonPropertyName("EmergencyStopHandle2")]
            public bool EmergencyStopHandle2 { get; set; }

            [JsonPropertyName("EmergencyStopHandle3")]
            public bool EmergencyStopHandle3 { get; set; }

            [JsonPropertyName("EmergencyStopHandle4")]
            public bool EmergencyStopHandle4 { get; set; }

            [JsonPropertyName("EmergencyStopHandle5")]
            public bool EmergencyStopHandle5 { get; set; }

            [JsonPropertyName("EmergencyStopHandle6")]
            public bool EmergencyStopHandle6 { get; set; }

            [JsonPropertyName("EmergencyStopHandle7")]
            public bool EmergencyStopHandle7 { get; set; }

            [JsonPropertyName("EmergencyStopHandle8")]
            public bool EmergencyStopHandle8 { get; set; }

            [JsonPropertyName("FireDetectState1")]
            public bool FireDetectState1 { get; set; }

            [JsonPropertyName("FireDetectState2")]
            public bool FireDetectState2 { get; set; }
        }

    }
}




