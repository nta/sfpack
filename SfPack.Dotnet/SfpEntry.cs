using System;
using System.Runtime.InteropServices;

namespace SfPack.Dotnet
{
    /// <summary>
    ///  Generic struct of sfp file entry.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SfpEntry
    {
        public Int32 Magic { get; private set; }
        public Int64 NameOffset { get; private set; }
        public Int32 Unknown1 { get; private set; }
        public Int64 ParentOffset { get; private set; }
        public Int32 IsDir { get; private set; }
        public Int64 FileLength { get; private set; }
        public Int64 ModifiedTime { get; private set; }
        public Int64 CreatedTime { get; private set; }
        public Int64 Unknown2 { get; private set; }
        public Int64 Unknown3 { get; private set; }
        public Int64 StartOffset { get; private set; }
        public Int32 DataLength { get; private set; }

        /// <summary>
        /// Builds a sfp entry from its raw bytes.
        /// </summary>
        /// <param name="data">Raw bytes.</param>
        /// <returns>Sfp entry.</returns>
        internal static unsafe SfpEntry CreateFromBytes(Byte[] data)
        {
            SfpEntry _directory;

            if (data == null || data.Length != sizeof(SfpEntry))
                throw new Exception("INVALID SFP ENTRY DATA");

            fixed (Byte* map = &data[0])
                _directory = *(SfpEntry*)map;

            return _directory;
        }
        /// <summary>
        /// Writes into the console human readable information about sfp file entry.
        /// </summary>
        internal void Print()
        {
            Console.WriteLine($"magic: {Magic}");
            Console.WriteLine($"nameOffset: {NameOffset}");
            Console.WriteLine($"unk1: {Unknown1}");
            Console.WriteLine($"parentOffset: {ParentOffset}");
            Console.WriteLine($"isDir: {IsDir}");
            Console.WriteLine($"fileLength: {FileLength}");
            Console.WriteLine($"modifiedTime: {ModifiedTime}");
            Console.WriteLine($"createdTime: {CreatedTime}");
            Console.WriteLine($"unk2: {Unknown2}");
            Console.WriteLine($"unk3: {Unknown3}");
            Console.WriteLine($"startOffset: {StartOffset}");
            Console.WriteLine($"dataLength: {DataLength}");
        }
    }
}
