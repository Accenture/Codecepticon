using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codecepticon.CommandLine;
using Codecepticon.Utils;

namespace Codecepticon.Modules.VB6.CommandLine
{
    class Vb6CommandLine : CommandLineManager
    {
        public Vb6CommandLine(string[] args) : base(args)
        {
            Arguments = new Dictionary<string, string>();
            MergeArguments();
        }

        protected override void SetRenameValue(bool value)
        {
            CommandLineData.Vb6.Rename.Identifiers = value;
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
                        CommandLineData.Vb6.Rename.Enabled = (argument.Value.Length > 0);
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
                        case "i":
                            CommandLineData.Vb6.Rename.Identifiers = true;
                            break;
                    }
                }
            }
        }
    }
}
