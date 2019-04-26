using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SfPack.Dotnet
{
    class Program
    {
        static void Main(String[] args)
        {
            String[] path;
            switch (args.Length)
            {
                case 0:
                    Console.WriteLine("Please specify a path of file or directory.");
                    break;
                case 1:
                    //Process a file or all *.sfp into a directory.
                    path = GetPathExtension(args[0]);
                    if (!String.IsNullOrEmpty(path[0]))
                        if (Directory.Exists(path[0]))
                            foreach (FileInfo file in new DirectoryInfo(path[0]).GetFiles(path[1]))
                                SfpFile.ExtractFile(file.FullName);
                        else
                            SfpFile.ExtractFile(path[0]);
                    else
                        Console.WriteLine("The file or directory is not valid.");
                    break;
                default:
                    Console.WriteLine("Call has some invalid arguments.");
                    break;
            }
        }
        /// <summary>
        /// Extracts full path of the file or directory and the search pattern.
        /// </summary>
        /// <param name="path">Raw path.</param>
        /// <returns>Array [Full path, Search pattern].</returns>
        private static String[] GetPathExtension(String path)
        {
            String[] ext = new String[2];

            if (Regex.IsMatch(path, "^\".+\"$"))
                ext[0] = path.Substring(1, path.Length - 2);
            else
                ext[0] = path;
            ext[1] = "*.sfp";
            try
            {
                ext[0] = Regex.Replace(ext[0], @"^.+(\*(.\w+){0,1})$", (m) =>
                {
                    ext[1] = m.Groups[1].Value;
                    return m.Value.Replace(ext[1], "");
                });
                ext[0] = Path.GetFullPath(ext[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}.");
                ext[0] = null;
            }
            return ext;
        }
    }
}
