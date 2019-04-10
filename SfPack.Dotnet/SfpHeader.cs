using System;
using System.Runtime.InteropServices;

namespace SfPack.Dotnet
{
    /// <summary>
    /// Generic struct of sfp file header.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SfpHeader
    {
        public Int32 Magic { get; private set; }
        public Int32 Version { get; private set; }
        public Int64 Unknown1 { get; private set; }
        public Int64 FirstEntryOffset { get; private set; }
        public Int64 NameTableOffset { get; private set; }
        public Int64 DataOffset { get; private set; }
        public Int64 ArchiveSize { get; private set; }
        public Int64 PackageLabelOffset { get; private set; }
        public Int64 Unknown3 { get; private set; }

        /// <summary>
        /// Builds a sfp header from its raw bytes.
        /// </summary>
        /// <param name="data">Raw bytes.</param>
        /// <returns>Sfp header.</returns>
        internal static unsafe SfpHeader CreateFromBytes(Byte[] data)
        {
            SfpHeader _header;

            if (data == null || data.Length != sizeof(SfpHeader))
                throw new Exception("INVALID SFP HEADER DATA");

            fixed (Byte* map = &data[0])
                _header = *(SfpHeader*)map;

            return _header;
        }
        /// <summary>
        /// Writes into the console human readable information about sfp file header.
        /// </summary>
        internal void Print()
        {
            Console.WriteLine($"magic: {Magic}");
            Console.WriteLine($"version: {Version}");
            Console.WriteLine($"unk1: {Unknown1}");
            Console.WriteLine($"firstDirOffset: {FirstEntryOffset}");
            Console.WriteLine($"dataOffset: {DataOffset}");
            Console.WriteLine($"archiveSize: {ArchiveSize}");
            Console.WriteLine($"packageLabelOffset: {PackageLabelOffset}");
            Console.WriteLine($"unk3: {Unknown3}");
        }
    }
}
