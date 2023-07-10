using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Utils;


namespace Codecepticon.Modules.CSharp
{
    class Unmapping : DataUnmapping
    {
        public static void GenerateMapFile(string saveAs)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                ["namespaces"] = "",
                ["classes"] = "",
                ["functions"] = "",
                ["enums"] = "",
                ["properties"] = "",
                ["variables"] = "",
                ["parameters"] = "",
                ["structs"] = "",
                ["cmdline"] = "",
            };

            if (CommandLineData.CSharp.Rename.Namespaces)
            {
                data["namespaces"] = ConcatData(DataCollector.Mapping.Namespaces);
            }

            if (CommandLineData.CSharp.Rename.Classes)
            {
                data["classes"] = ConcatData(DataCollector.Mapping.Classes);
            }

            if (CommandLineData.CSharp.Rename.Functions)
            {
                data["functions"] = ConcatData(DataCollector.Mapping.Functions);
            }

            if (CommandLineData.CSharp.Rename.Enums)
            {
                data["enums"] = ConcatData(DataCollector.Mapping.Enums);
            }

            if (CommandLineData.CSharp.Rename.Properties)
            {
                data["properties"] = ConcatData(DataCollector.Mapping.Properties);
            }

            if (CommandLineData.CSharp.Rename.Variables)
            {
                data["variables"] = ConcatData(DataCollector.Mapping.Variables);
            }

            if (CommandLineData.CSharp.Rename.Parameters)
            {
                data["parameters"] = ConcatData(DataCollector.Mapping.Parameters);
            }

            if (CommandLineData.CSharp.Rename.Structs)
            {
                data["structs"] = ConcatData(DataCollector.Mapping.Structs);
            }

            if (CommandLineData.CSharp.Rename.CommandLine)
            {
                data["cmdline"] = DataCollector.ConcatCommandLineData(DataCollector.Mapping.CommandLine);
            }

            WriteTemplateFile(saveAs, data);
        }
    }
}
