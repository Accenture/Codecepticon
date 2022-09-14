using Codecepticon.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.PowerShell
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
                ["cmdline"] = "",
            };

            if (CommandLineData.PowerShell.Rename.Functions)
            {
                data["functions"] = ConcatData(DataCollector.Mapping.Functions);
            }

            if (CommandLineData.PowerShell.Rename.Variables || CommandLineData.PowerShell.Rename.Parameters)
            {
                data["variables"] = ConcatData(DataCollector.Mapping.Variables);
            }

            WriteTemplateFile(saveAs, data);
        }
    }
}
