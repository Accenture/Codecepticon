using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.VB6
{
    class OfficeReservedWords
    {
        public static List<string> FileMacroFunctions = new List<string>
        {
            /* Word */
            "autoopen",
            "autonew",
            "autoclose",
            "autoexec",
            "autoexit",
            /* Excel */
            "auto_open",
            "auto_close",
            "auto_activate",
            "auto_deactivate"
        };
    }
}
