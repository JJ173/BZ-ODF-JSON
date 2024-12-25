using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;

namespace BZ_ODF_JSON
{
    class Program
    {
        private const int BZCCAPPID = 624970;
        private const int VSRMODID = 1325933293;
        private const string steam32 = "SOFTWARE\\VALVE\\";
        private const string steam64 = "SOFTWARE\\Wow6432Node\\Valve\\";

        [SupportedOSPlatform("windows")]
        public static void Main(string[] args)
        {
            // Test Steam32 first, if that doesn't exist, switch to Steam64.
            var steamKey = Registry.LocalMachine.OpenSubKey(steam32);
            steamKey ??= Registry.LocalMachine.OpenSubKey(steam64);

            // Return early if the steamKey is null.
            if (steamKey == null)
            {
                Console.WriteLine("Error: Unable to find Steam installation.");
                return;
            }

            foreach (var subKeyName in steamKey.GetSubKeyNames())
            {
                using (var subKey = steamKey.OpenSubKey(subKeyName))
                {
                    if (subKey == null)
                    {
                        break;
                    }

                    var steamPath = subKey.GetValue("InstallPath");

                    if (steamPath == null)
                    {
                        break;
                    }

                    var steamPathString = steamPath.ToString();
                    var baseGamePath = $"{steamPathString}\\steamapps\\common\\BZ2R";
                    var vsrModPath = $"{steamPathString}\\steamapps\\workshop\\content\\{BZCCAPPID}\\{VSRMODID}";

                    // Paths to process.
                    var baseGamePathProcess = $"{baseGamePath}\\bz2r_res\\baked";
                    var vsrPathToProcess = $"{vsrModPath}\\VSR";

                    // Find any ODF files.
                    var stockODFFiles = FileHelper.GetFiles(baseGamePathProcess, "*.odf");
                    var vsrODFFiles = FileHelper.GetFiles(vsrPathToProcess, "*.odf");

                    // Filter for the files that we need for relevance.
                    string[] inclusions = ["fv", "fb", "iv", "ib", "ev", "eb", "cv", "cb"];

                    // Filter out files that aren't relevant.
                    List<Dictionary<string, string>> odfFiles =
                    [
                        .. stockODFFiles.Where(s => inclusions.Any(prefix => s.First().Key.StartsWith(prefix))).ToList(),
                        .. vsrODFFiles.Where(s => inclusions.Any(prefix => s.First().Key.StartsWith(prefix))).ToList(),
                    ];

                    if (odfFiles == null || odfFiles.Count <= 0)
                    {
                        Console.WriteLine("Error: Unable to find any ODF files for BZCC to process.");
                        break;
                    }

                    // Keep track of the new file path for JSON.
                    string jsonFilePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/BZCC-ODF-Data.json";

                    // Check to see if the file exists first and remove it. 
                    if (File.Exists(jsonFilePath))
                    {
                        File.Delete(jsonFilePath);
                    }

                    // If any files exist, create a JSON file on the user disk.
                    var file = File.Create(jsonFilePath);

                    // Close the file.
                    file.Close();

                    // Create a StringBuilder for the JSON Content.
                    StringBuilder sb = new();
                    sb.Append('{');

                    // Run through each ODF file, convert it to JSON, add it to the file.
                    for (int i = 0; i < odfFiles.Count; i++)
                    {
                        var odfFile = odfFiles[i].First();

                        // See if we can open the file from the path that is part of the dictionary.
                        string contents = File.ReadAllText(odfFile.Value);

                        // Test to see if we can convert that to JSON.
                        string jsonContents = FileHelper.GetIniAsJson(odfFile.Key, contents, false, i == odfFiles.Count - 1);

                        // Append the contents to a new line in the StringBuilder.
                        sb.AppendLine(jsonContents);
                    }

                    sb.Append('}');

                    // Append that to the file we just wrote to the disk.
                    File.AppendAllText(jsonFilePath, sb.ToString());
                }
            }
        }
    }
}