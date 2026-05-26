namespace MissionScannerGUI
{
    internal sealed class MissionRow
    {
        public string DllName { get; set; }
        public string Type { get; set; }          // Cam/Col/Dem/Tut/MLR/MSP/MRR/MM/ML
        public string Players { get; set; }
        public string UnitOnly { get; set; }      // T/F
        public string MapName { get; set; }
        public string TechtreeName { get; set; }
        public string Description { get; set; }
    }
}
