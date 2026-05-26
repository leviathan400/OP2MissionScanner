using System.IO;

namespace OP2MissionScanner
{
    internal sealed class MissionDll
    {
        public string DllName { get; private set; }
        public string MapName { get; private set; }
        public string TechtreeName { get; private set; }
        public string LevelDesc { get; private set; }

        // Returns null if the file is not an Outpost 2 mission DLL.
        // Detection rule: must be a 32-bit PE DLL exporting "LevelDesc".
        public static MissionDll TryLoad(string path)
        {
            var pe = PeExportReader.TryOpen(path);
            if (pe == null || !pe.HasExport("LevelDesc")) return null;

            return new MissionDll
            {
                DllName = Path.GetFileName(path),
                MapName = pe.ReadExportString("MapName") ?? string.Empty,
                TechtreeName = pe.ReadExportString("TechtreeName") ?? string.Empty,
                LevelDesc = pe.ReadExportString("LevelDesc") ?? string.Empty,
            };
        }
    }
}
