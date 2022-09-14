using Codecepticon.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.VB6
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

            if (CommandLineData.Vb6.Rename.Identifiers)
            {
                data["functions"] = ConcatData(DataCollector.Mapping.Identifiers);
            }

            WriteTemplateFile(saveAs, data);
        }
    }
}
