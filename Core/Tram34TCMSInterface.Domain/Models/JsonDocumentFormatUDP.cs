namespace Tram34TCMSInterface.Domain.Models
{
    public class JsonDocumentFormatUDP
    {
        public class CouplingTrain
        {
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
            public int ID { get; set; }
            public string IP { get; set; }
            public bool CabAActive { get; set; }
            public bool CabBActive { get; set; }
            public bool CabineAKeyStatus { get; set; }
            public bool CabineBKeyStatus { get; set; }
            public int TrainCoupledOrder { get; set; }
            public bool IsTrainCoupled { get; set; }
            public bool AllDoorOpen { get; set; }
            public bool AllDoorClose { get; set; }
            public bool AllDoorReleased { get; set; }
            public bool AllLeftDoorOpen { get; set; }
            public bool AllRightDoorOpen { get; set; }
            public bool AllLeftDoorClose { get; set; }
            public bool AllRightDoorClose { get; set; }
            public bool AllLeftDoorReleased { get; set; }
            public bool AllRightDoorReleased { get; set; }
        }

        public class TrainData
        {
            public string TimeStamp { get; set; }
            public int MasterTrainId { get; set; }
            public int TrainSpeed { get; set; }
            public bool ZeroSpeed { get; set; }
            public bool TachoMeterPulse { get; set; }
            public string Date { get; set; }
            public string Time { get; set; }

            // CouplingTrainsId'yi CouplingTrainsIds olarak düzelttim.
            public List<CouplingTrain> CouplingTrainsIds { get; set; }

            public List<Train> TRAIN { get; set; }
        }
    }
}
