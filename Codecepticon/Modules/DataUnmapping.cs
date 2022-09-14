using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Utils;
using Newtonsoft.Json;
using System.Security;


namespace Codecepticon.Modules
{
    class DataUnmapping
    {
        protected bool UnmapFiles(Dictionary<string, string> mapping, List<string> inputFiles)
        {
            foreach (string file in inputFiles)
            {
                Logger.Info($"Unmapping file {file}");
                string contents = File.ReadAllText(file);
                foreach (KeyValuePair<string, string> map in mapping)
                {
                    contents = contents.Replace(map.Key, map.Value);
                }
                File.WriteAllText(file, contents);
            }
            return true;
        }

        protected Dictionary<string, string> LoadMappingFromFile(string mapFile)
        {
            Dictionary<string, string> mapping = new Dictionary<string, string>();

            string jsonData = ExtractJsonDataFromHtml(File.ReadAllText(mapFile));
            if (String.IsNullOrEmpty(jsonData))
            {
                return mapping;
            }

            Dictionary<string, string> rawMapping = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);

            foreach (KeyValuePair<string, string> item in rawMapping)
            {
                string[] data = item.Value.Split(new[] { "|" }, StringSplitOptions.None);
                foreach (string value in data)
                {
                    string[] parts = value.Split(new[] { ":" }, StringSplitOptions.None);
                    if (parts.Length == 2 && !mapping.ContainsKey(parts[1]))
                    {
                        mapping.Add(parts[1], parts[0]);
                    }
                }
            }

            return mapping;
        }

        private string ExtractJsonDataFromHtml(string html)
        {
            string mapSectionStart = "<!-- MAPPING_DATA_START -->";
            string mapSectionEnd = "<!-- MAPPING_DATA_END -->";

            int mappingStart = html.IndexOf(mapSectionStart);
            int mappingEnd = html.IndexOf(mapSectionEnd, mappingStart);
            if (mappingStart == -1 || mappingEnd == -1)
            {
                Logger.Error("Could not find mapping data in HTML");
                Logger.Error($"mappingStart is {mappingStart} and mappingEnd is {mappingEnd}");
                return "";
            }
            html = html.Substring(mappingStart, mappingEnd - mappingStart).Trim();
            
            // Now we need to get the contents of the <script> tag.
            mappingStart = html.IndexOf("<script");
            mappingEnd = html.IndexOf(">", mappingStart);
            if (mappingStart == -1 || mappingEnd == -1 || mappingEnd < mappingStart)
            {
                Logger.Error("Could not find mapping JSON in HTML");
                Logger.Error($"mappingStart is {mappingStart} and mappingEnd is {mappingEnd}");
                return "";
            }

            html = html.Substring(mappingEnd + 1).Trim();
            mappingEnd = html.IndexOf("</script>");
            html = html.Substring(0, mappingEnd).Trim();

            return html;
        }

        protected static string ConcatData(IDictionary<string, string> mapping)
        {
            string data = "";
            foreach (KeyValuePair<string, string> item in mapping)
            {
                if (data.Length > 0)
                {
                    data += "|";
                }
                data += $"{item.Key}:{item.Value}";
            }
            return data;
        }

        public bool Run(string mapFile, string unmapFile, string unmapDirectory, bool unmapRecursive)
        {
            Logger.Info("Loading mapping data...", false);
            Dictionary<string, string> mapping = LoadMappingFromFile(mapFile);
            List<string> inputFiles = GetInputFiles(unmapFile, unmapDirectory, unmapRecursive);
            Logger.Info("", true, false);

            if (!mapping.Any())
            {
                Logger.Error("No mapping data loaded.");
                return false;
            }
            
            if (!inputFiles.Any())
            {
                Logger.Error("No input files found.");
                return false;
            }
            return UnmapFiles(mapping, inputFiles);
        }

        protected List<string> GetInputFiles(string unmapFile, string unmapDirectory, bool unmapRecursive)
        {
            List<string> inputFiles = new List<string>();

            if (unmapFile != "" && File.Exists(unmapFile))
            {
                inputFiles.Add(Path.GetFullPath(unmapFile));
            }
            else if (unmapDirectory != "" && Directory.Exists(unmapDirectory))
            {
                SearchOption isRecursive = unmapRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                string[] files = Directory.GetFiles(unmapDirectory, "*.*", isRecursive);
                inputFiles.AddRange(files.Select(file => Path.GetFullPath(file)));
            }

            return inputFiles;
        }

        protected static void WriteTemplateFile(string saveAs, Dictionary<string, string> data)
        {
            string templateContents = "";
            string templateFile = Path.GetFullPath(@".\Templates\Mapping.html");
            templateContents = File.ReadAllText(templateFile);

            foreach (KeyValuePair<string, string> mapping in data)
            {
                templateContents = templateContents.Replace($"%{mapping.Key}%", mapping.Value);
            }

            // Write the command line used to obfuscate.
            templateContents = templateContents.Replace("%fullcommandline%", String.Join(" ", CommandLineData.Global.RawCmdLineArgs));
            templateContents = templateContents.Replace("%rawconfig%", SecurityElement.Escape(CommandLineData.Global.RawConfigFile));
            templateContents = templateContents.Replace("%rawconfigvisibility%", String.IsNullOrEmpty(CommandLineData.Global.RawConfigFile) ? "d-none" : "");

            // Make sure the output path exists.
            string path = Path.GetDirectoryName(saveAs);
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    // Now check again.
                    if (!Directory.Exists(path))
                    {
                        throw new Exception("Could not create path");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Could not create path to save the HTML mapping file: {path}");
                Logger.Error(e.Message);
                return;
            }

            File.WriteAllText(saveAs, templateContents);
        }
    }
}
