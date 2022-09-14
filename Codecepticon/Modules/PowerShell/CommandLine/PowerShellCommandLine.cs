using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codecepticon.CommandLine;
using Codecepticon.Utils;

namespace Codecepticon.Modules.PowerShell.CommandLine
{
    class PowerShellCommandLine : CommandLineManager
    {
        public PowerShellCommandLine(string[] args) : base(args)
        {
            Arguments = new Dictionary<string, string>();
            MergeArguments();
        }

        protected override bool Parse(Dictionary<string, string> arguments)
        {
            if (!ParseGlobalArguments(arguments))
            {
                return false;
            }

            foreach (KeyValuePair<string, string> argument in arguments)
            {
                switch (argument.Key.ToLower())
                {
                    case "rename":
                        CommandLineData.PowerShell.Rename.Enabled = (argument.Value.Length > 0);
                        ParseRenameOptions(argument.Value.ToLower());
                        break;
                    //default:
                    //    Logger.Error($"Code error: Not supported parameter - {argument.Key}");
                    //    return false;
                }
            }

            ValidateCommandLine validate = new ValidateCommandLine();
            return validate.Run();
        }

        protected void ParseRenameOptions(string value)
        {
            if (value == "all")
            {
                SetRenameValue(true);
            }
            else
            {
                SetRenameValue(false);
                foreach (char c in value.ToLower().ToArray())
                {
                    switch (c.ToString())
                    {
                        case "f":
                            CommandLineData.PowerShell.Rename.Functions = true;
                            break;
                        case "v":
                            CommandLineData.PowerShell.Rename.Variables = true;
                            CommandLineData.PowerShell.Rename.Parameters = true;
                            break;
                    }
                }
            }
        }

        protected override void SetRenameValue(bool value)
        {
            CommandLineData.PowerShell.Rename.Functions = value;
            CommandLineData.PowerShell.Rename.Variables = value;
            CommandLineData.PowerShell.Rename.Parameters = value;
        }
    }
}
