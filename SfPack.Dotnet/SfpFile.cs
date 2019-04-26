using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SfPack.Dotnet
{
    /// <summary>
    /// Static class with utilities to process sfp files.
    /// </summary>
    public static class SfpFile
    {
        /// <summary>
        /// Extracts the content of a sfp file from its path.
        /// </summary>
        /// <param name="filePath">Sfp file path.</param>
        /// <param name="overwrite">Indicates whether the files should be overwritten.</param>
        /// <param name="extractPath">Extraction path.</param>
        public static void ExtractFile(String filePath, Boolean overwrite = true, String extractPath = null)
        {
            FileInfo file;
            DirectoryInfo dir;
            file = new FileInfo(filePath);
            Console.WriteLine($"{file.FullName} {(!file.Exists ? "no " : "")}found.");
            if (file.Exists)
            {
                if (String.IsNullOrWhiteSpace(extractPath))
                    extractPath = file.FullName.Replace(file.Extension, "");
                else
                {
                    dir = new DirectoryInfo(extractPath);
                    if (!dir.Exists)
                        dir.Create();
                    extractPath = new DirectoryInfo(extractPath).FullName;
                }
                using (FileStream fs = file.OpenRead())
                    ExtractStream(fs, overwrite, extractPath);
            }
        }

        /// <summary>
        /// Extract the content of a sfp file from a stream. 
        /// </summary>
        /// <param name="strm">Sfp stream data.</param>
        /// <param name="overwrite">Indicates whether the files should be overwritten.</param>
        /// <param name="extractPath">Extraction path.</param>
        private static void ExtractStream(Stream strm, Boolean overwrite, String extractPath)
        {
            SfpHeader header = ReadHeader(strm);
            if (header.ArchiveSize == strm.Length) //1st Validation
            {
                IReadOnlyDictionary<Int64, String> nameTable = ReadNameTable(strm, header);
                if (!nameTable.ContainsKey(0))    //2nd Validation
                {
                    Dictionary<Int64, SfpEntry> entries = new Dictionary<Int64, SfpEntry>();
                    ReadEntry(strm, header.FirstEntryOffset, entries);
                    ExtractFiles(strm, nameTable, entries, overwrite, extractPath);
                }
                else
                    Console.WriteLine("Selected file is not a valid sfp. An error found in the file name table.");
            }
            else
                Console.WriteLine("Selected file is not a valid sfp. An error found in the file header.");

        }
        /// <summary>
        /// Read from stream the sfp file header.
        /// </summary>
        /// <param name="strm">Sfp stream data.</param>
        /// <returns>Sfp file header.</returns>
        private static unsafe SfpHeader ReadHeader(Stream strm)
        {
            Byte[] data = new Byte[sizeof(SfpHeader)];
            strm.Seek(0, SeekOrigin.Begin);
            strm.Read(data, 0, data.Length);
            return SfpHeader.CreateFromBytes(data);
        }
        /// <summary>
        /// Read the table of names in the sfp file and index them by offset.
        /// </summary>
        /// <param name="strm">Sfp stream data.</param>
        /// <param name="header">Sfp file header</param>
        /// <returns>Sfp file name table indexed by offset.</returns>
        private static IReadOnlyDictionary<Int64, String> ReadNameTable(Stream strm, SfpHeader header)
        {
            Dictionary<Int64, String> nameTable = new Dictionary<Int64, String>();
            Byte[] data = new Byte[header.DataOffset - header.NameTableOffset];
            strm.Seek(header.NameTableOffset, SeekOrigin.Begin);
            strm.Read(data, 0, data.Length);

            Int32 pos_ini = 0;
            Int32 pos_fin = 0;
            List<Byte> lista = data.ToList<Byte>();
            foreach(Byte bytChar in lista)
            {
                pos_fin++;
                if (bytChar == 0)
                {
                    if(pos_fin - pos_ini > 1)
                        nameTable.Add(header.NameTableOffset + (Int64)pos_ini, Encoding.ASCII.GetString(lista.GetRange(pos_ini, pos_fin - pos_ini - 1).ToArray()));                    
                    pos_ini = pos_fin;
                }
            }
            return nameTable;
        }
        /// <summary>
        /// Reads recursively all the entries contained in the sfp file.
        /// </summary>
        /// <param name="strm">Sfp stream data.</param>
        /// <param name="offset_ent">Sfp entry offset.</param>
        /// <param name="entries">Sfp entries indexed by offset.</param>
        private unsafe static void ReadEntry(Stream strm, Int64 offset_ent, Dictionary<Int64, SfpEntry> entries)
        {
            SfpEntry entry;
            Byte[] data;
            strm.Seek(offset_ent, SeekOrigin.Begin);
            data = new Byte[sizeof(SfpEntry)];
            strm.Read(data, 0, data.Length);
            entry = SfpEntry.CreateFromBytes(data);
            entries.Add(offset_ent, entry);
            if (entry.IsDir == 1)
                for (Int64 offset = entry.StartOffset; offset < (entry.StartOffset + entry.DataLength); offset += data.Length)
                    ReadEntry(strm, offset, entries);
        }
        /// <summary>
        /// Creates the relative path of an entry in the sfp file.
        /// </summary>
        /// <param name="entries">Sfp entries indexed by offset.</param>
        /// <param name="nameTable">Sfp file name table indexed by offset.</param>
        /// <param name="offset">Sfp entry offset.</param>
        /// <returns>Relative path of sfp entry.</returns>
        private static String BuildPath(IReadOnlyDictionary<Int64, SfpEntry> entries, IReadOnlyDictionary<Int64, String> nameTable, Int64 offset)
        {
            SfpEntry entry = entries[offset];
            String path = "";
            do
            {
                if(entry.NameOffset != 0)
                    path = $"{nameTable[entry.NameOffset]}{path}";
                offset = entry.ParentOffset;
                entry = entries[offset];
                path = $"{Path.DirectorySeparatorChar}{path}";
            } while (offset != entry.ParentOffset);
            return path;
        }
        /// <summary>
        /// Creates a file or directory which represent a sfp entry.
        /// </summary>
        /// <param name="strm">Sfp stream data.</param>
        /// <param name="nameTable">Sfp file name table indexed by offset.</param>
        /// <param name="entries">Sfp entries indexed by offset.</param>
        /// <param name="overwrite">Indicates whether the files should be overwritten.</param>
        /// <param name="extractPath">Extraction path.</param>
        private static void ExtractFiles(Stream strm, IReadOnlyDictionary<Int64, String> nameTable, IReadOnlyDictionary<Int64, SfpEntry> entries, Boolean overwrite, String extractPath)
        {
            DirectoryInfo dir = null;
            FileInfo file = null;
            Byte[] data;

            String path;
            Int64 directoryCount = 0;
            Int64 filesCount = 0;

            foreach (KeyValuePair<Int64, SfpEntry> pair in entries)
            {
                path = $"{extractPath}{BuildPath(entries, nameTable, pair.Key)}";
                try
                {
                    if (pair.Value.IsDir == 1)
                    {
                        dir = new DirectoryInfo(path);
                        if (!dir.Exists)
                            dir.Create();
                        directoryCount++;
                        dir = null;
                    }
                    else if (pair.Value.IsDir == 0)
                    {
                        file = new FileInfo(path);
                        if (file.Exists && overwrite)
                        {
                            file.Delete();
                            file.Refresh();
                        }
                        if (!file.Exists)
                        {
                            using (FileStream fs = file.Create())
                            using (BinaryWriter wrt = new BinaryWriter(fs))
                            {
                                data = new Byte[pair.Value.DataLength];
                                strm.Seek(pair.Value.StartOffset, SeekOrigin.Begin);
                                strm.Read(data, 0, data.Length);
                                wrt.Write(data);
                                data = null;
                            }
                            filesCount++;
                        }
                        file = null;
                    }
                    else
                    {
                        throw new Exception("Invalid directory flag value");
                    }
                    Console.WriteLine($"{path} {(pair.Value.IsDir == 0 ? $".. {pair.Value.DataLength} bytes.":"")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}.");
                    Console.WriteLine($"{path} could not be processed.");
                }
            }
            Console.WriteLine($"{filesCount} file{(filesCount == 1? "":"s")} extracted into {directoryCount} folder{(directoryCount == 1 ? "" : "s")}.");
        }
    }
}
