using System;
using System.IO;
using System.Text;

namespace OP2MissionScanner
{
    // Reads named exports from a 32-bit PE DLL.
    // One source of truth for getting strings out of an Outpost 2 mission DLL —
    // no heuristic byte scanning anywhere else in the codebase.
    internal sealed class PeExportReader
    {
        private readonly byte[] _data;
        private readonly Section[] _sections;
        private readonly string[] _exportNames;     // index = ordinal offset
        private readonly uint[] _exportAddressRvas; // index = ordinal offset

        private PeExportReader(byte[] data, Section[] sections, string[] names, uint[] addressRvas)
        {
            _data = data;
            _sections = sections;
            _exportNames = names;
            _exportAddressRvas = addressRvas;
        }

        // Returns null if the file is not a 32-bit PE DLL with an export table.
        public static PeExportReader TryOpen(string path)
        {
            byte[] data;
            try { data = File.ReadAllBytes(path); }
            catch { return null; }

            if (data.Length < 0x40) return null;

            // DOS header -> PE signature offset at 0x3C
            int peSigOffset = BitConverter.ToInt32(data, 0x3C);
            if (peSigOffset < 0 || peSigOffset + 24 > data.Length) return null;
            if (data[peSigOffset] != 'P' || data[peSigOffset + 1] != 'E' ||
                data[peSigOffset + 2] != 0 || data[peSigOffset + 3] != 0) return null;

            int coff = peSigOffset + 4; // COFF header
            ushort numberOfSections = BitConverter.ToUInt16(data, coff + 2);
            ushort sizeOfOptionalHeader = BitConverter.ToUInt16(data, coff + 16);
            ushort characteristics = BitConverter.ToUInt16(data, coff + 18);
            if ((characteristics & 0x2000) == 0) return null; // not a DLL

            int optionalHeader = coff + 20;
            if (optionalHeader + 2 > data.Length) return null;
            ushort magic = BitConverter.ToUInt16(data, optionalHeader);
            if (magic != 0x10B) return null; // must be PE32 (32-bit)

            // PE32 optional header is 96 bytes; data directories follow.
            // Export Directory is data directory entry [0] at offset 96.
            int dataDirs = optionalHeader + 96;
            if (dataDirs + 8 > data.Length) return null;
            uint exportRva = BitConverter.ToUInt32(data, dataDirs);
            uint exportSize = BitConverter.ToUInt32(data, dataDirs + 4);
            if (exportRva == 0 || exportSize == 0) return null;

            int sectionTableOffset = optionalHeader + sizeOfOptionalHeader;
            var sections = ReadSections(data, sectionTableOffset, numberOfSections);
            if (sections == null) return null;

            if (!TryRvaToFileOffset(sections, exportRva, out int exportDirOffset)) return null;
            if (exportDirOffset + 40 > data.Length) return null;

            // ExportDirectoryTable layout (40 bytes):
            //   0x14 numberOfNamePointers (uint)
            //   0x1C exportAddressTableRva (uint)
            //   0x20 namePointerRva (uint)
            //   0x24 ordinalTableRva (uint)
            uint numberOfNamePointers = BitConverter.ToUInt32(data, exportDirOffset + 0x18);
            uint exportAddressTableRva = BitConverter.ToUInt32(data, exportDirOffset + 0x1C);
            uint namePointerRva = BitConverter.ToUInt32(data, exportDirOffset + 0x20);
            uint ordinalTableRva = BitConverter.ToUInt32(data, exportDirOffset + 0x24);

            if (numberOfNamePointers == 0) return null;

            if (!TryRvaToFileOffset(sections, namePointerRva, out int namePtrOffset)) return null;
            if (!TryRvaToFileOffset(sections, ordinalTableRva, out int ordinalOffset)) return null;
            if (!TryRvaToFileOffset(sections, exportAddressTableRva, out int eatOffset)) return null;

            var names = new string[numberOfNamePointers];
            var addressRvas = new uint[numberOfNamePointers];

            for (int i = 0; i < numberOfNamePointers; i++)
            {
                int npEntry = namePtrOffset + i * 4;
                if (npEntry + 4 > data.Length) return null;
                uint nameRva = BitConverter.ToUInt32(data, npEntry);
                if (!TryRvaToFileOffset(sections, nameRva, out int nameOffset)) return null;
                names[i] = ReadNullTerminated(data, nameOffset);

                int ordEntry = ordinalOffset + i * 2;
                if (ordEntry + 2 > data.Length) return null;
                ushort ordinalIndex = BitConverter.ToUInt16(data, ordEntry);

                int eatEntry = eatOffset + ordinalIndex * 4;
                if (eatEntry + 4 > data.Length) return null;
                addressRvas[i] = BitConverter.ToUInt32(data, eatEntry);
            }

            return new PeExportReader(data, sections, names, addressRvas);
        }

        public bool HasExport(string name)
        {
            return IndexOf(name) >= 0;
        }

        public string ReadExportString(string name)
        {
            int idx = IndexOf(name);
            if (idx < 0) return null;
            if (!TryRvaToFileOffset(_sections, _exportAddressRvas[idx], out int offset)) return null;
            return ReadNullTerminated(_data, offset);
        }

        private int IndexOf(string name)
        {
            for (int i = 0; i < _exportNames.Length; i++)
            {
                if (string.Equals(_exportNames[i], name, StringComparison.Ordinal)) return i;
            }
            return -1;
        }

        private static Section[] ReadSections(byte[] data, int offset, int count)
        {
            const int sectionSize = 40;
            if (offset + count * sectionSize > data.Length) return null;
            var sections = new Section[count];
            for (int i = 0; i < count; i++)
            {
                int s = offset + i * sectionSize;
                sections[i] = new Section
                {
                    VirtualSize = BitConverter.ToUInt32(data, s + 8),
                    VirtualAddress = BitConverter.ToUInt32(data, s + 12),
                    SizeOfRawData = BitConverter.ToUInt32(data, s + 16),
                    PointerToRawData = BitConverter.ToUInt32(data, s + 20),
                };
            }
            return sections;
        }

        private static bool TryRvaToFileOffset(Section[] sections, uint rva, out int fileOffset)
        {
            foreach (var s in sections)
            {
                uint extent = Math.Max(s.VirtualSize, s.SizeOfRawData);
                if (rva >= s.VirtualAddress && rva < s.VirtualAddress + extent)
                {
                    fileOffset = (int)(rva - s.VirtualAddress + s.PointerToRawData);
                    return true;
                }
            }
            fileOffset = 0;
            return false;
        }

        private static string ReadNullTerminated(byte[] data, int offset)
        {
            if (offset < 0 || offset >= data.Length) return string.Empty;
            int end = offset;
            while (end < data.Length && data[end] != 0) end++;
            return Encoding.ASCII.GetString(data, offset, end - offset);
        }

        private struct Section
        {
            public uint VirtualSize;
            public uint VirtualAddress;
            public uint SizeOfRawData;
            public uint PointerToRawData;
        }
    }
}
