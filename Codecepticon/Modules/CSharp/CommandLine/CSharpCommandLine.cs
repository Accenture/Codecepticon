using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codecepticon.CommandLine;
using Codecepticon.Modules.CSharp.Profiles;
using Codecepticon.Modules.CSharp.Profiles.Certify;
using Codecepticon.Modules.CSharp.Profiles.Rubeus;
using Codecepticon.Modules.CSharp.Profiles.Seatbelt;
using Codecepticon.Modules.CSharp.Profiles.SharpChrome;
using Codecepticon.Modules.CSharp.Profiles.SharpDPAPI;
using Codecepticon.Modules.CSharp.Profiles.SharpHound;
using Codecepticon.Modules.CSharp.Profiles.SharpView;
using Codecepticon.Utils;

namespace Codecepticon.Modules.CSharp.CommandLine
{
    class CSharpCommandLine : CommandLineManager
    {
        public CSharpCommandLine(string[] args) : base(args)
        {
            CommandLineData.CSharp.Profile = new BaseProfile();

            Arguments = new Dictionary<string, string>
            {
                { "build", "switch" },
                { "build-path", "" },
                { "profile", "" },
                { "precompile", "switch" }
            };

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
                        CommandLineData.CSharp.Rename.Enabled = (argument.Value.Length > 0);
                        ParseRenameOptions(argument.Value.ToLower());
                        break;
                    case "build":
                        if (argument.Value.ToLower() != "false")
                        {
                            CommandLineData.CSharp.Compilation.Build = (argument.Value.Length > 0);
                        }   
                        break;
                    case "build-path":
                        CommandLineData.CSharp.Compilation.OutputPath = argument.Value;
                        break;
                    case "profile":
                        ParseProfile(argument.Value.ToLower());
                        break;
                    case "precompile":
                        if (argument.Value.ToLower() != "false")
                        {
                            CommandLineData.CSharp.Compilation.Precompile = (argument.Value.Length > 0);
                        }
                        break;
                    //default:
                    //    Logger.Error($"Code error: Not supported parameter - {argument.Key}");
                    //    return false;
                }
            }

            ValidateCommandLine validate = new ValidateCommandLine();
            return validate.Run();
        }

        protected void ParseProfile(string name)
        {
            CommandLineData.CSharp.Profile = name switch {
                "rubeus" => new Rubeus(),
                "seatbelt" => new Seatbelt(),
                "sharphound" => new SharpHound(),
                "sharpview" => new SharpView(),
                "certify" => new Certify(),
                "sharpdpapi" => new SharpDPAPI(),
                "sharpchrome" => new SharpChrome(),
                _ => new BaseProfile()
            };
        }

        protected void ParseRenameOptions(string value)
        {
            switch (value)
            {
                case "all":
                    SetRenameValue(true);
                    break;
                case "none":
                    SetRenameValue(false);
                    break;
                default:
                    SetRenameValue(false);
                    foreach (char c in value.ToLower().ToArray())
                    {
                        switch (c.ToString())
                        {
                            case "n":
                                CommandLineData.CSharp.Rename.Namespaces = true;
                                break;
                            case "c":
                                CommandLineData.CSharp.Rename.Classes = true;
                                break;
                            case "e":
                                CommandLineData.CSharp.Rename.Enums = true;
                                break;
                            case "f":
                                CommandLineData.CSharp.Rename.Functions = true;
                                break;
                            case "p":
                                CommandLineData.CSharp.Rename.Properties = true;
                                break;
                            case "a":
                                CommandLineData.CSharp.Rename.Parameters = true;
                                break;
                            case "v":
                                CommandLineData.CSharp.Rename.Variables = true;
                                break;
                            case "o":
                                CommandLineData.CSharp.Rename.CommandLine = true;
                                break;
                            case "s":
                                CommandLineData.CSharp.Rename.Structs = true;
                                break;
                        }
                    }
                    break;
            }
        }

        protected override void SetRenameValue(bool value)
        {
            CommandLineData.CSharp.Rename.Namespaces = value;
            CommandLineData.CSharp.Rename.Classes = value;
            CommandLineData.CSharp.Rename.Functions = value;
            CommandLineData.CSharp.Rename.Enums = value;
            CommandLineData.CSharp.Rename.Properties = value;
            CommandLineData.CSharp.Rename.Variables = value;
            CommandLineData.CSharp.Rename.Parameters = value;
            CommandLineData.CSharp.Rename.CommandLine = value;
            CommandLineData.CSharp.Rename.Structs = value;
        }
    }
}
