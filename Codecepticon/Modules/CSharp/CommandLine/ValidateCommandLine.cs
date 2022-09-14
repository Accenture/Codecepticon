using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Utils;

namespace Codecepticon.Modules.CSharp.CommandLine
{
    class ValidateCommandLine : CommandLineValidator
    {
        public bool Run()
        {
            if (!ValidateAction())
            {
                return false;
            }

            if (!ProfileSpecificValidation())
            {
                return false;
            }

            return true;
        }

        protected bool ValidateAction()
        {
            switch (CommandLineData.Global.Action)
            {
                case CommandLineData.Action.None:
                    Logger.Error("'action' parameter not set");
                    return false;
                case CommandLineData.Action.Obfuscate:
                    return ValidateActionObfuscate();
                case CommandLineData.Action.Unmap:
                    return ValidateActionUnmap();
            }

            return false;
        }

        protected bool ValidateActionObfuscate()
        {
            if (!CommandLineData.CSharp.Rename.Enabled && !CommandLineData.Global.Rewrite.Strings)
            {
                Logger.Error("No rename or rewrite parameters set.");
                return false;
            }

            // Check if the solution exists.
            if (String.IsNullOrEmpty(CommandLineData.Global.Project.Path) || !File.Exists(CommandLineData.Global.Project.Path))
            {
                Logger.Error($"Input path is empty or does not exist: {CommandLineData.Global.Project.Path}");
                return false;
            }

            if (String.IsNullOrEmpty(CommandLineData.Global.Project.SaveAs))
            {
                // Default behaviour is to replace the actual file.
                CommandLineData.Global.Project.SaveAs = CommandLineData.Global.Project.Path;
            }

            if (CommandLineData.CSharp.Rename.Enabled && !ValidateRename())
            {
                return false;
            }   

            if (!ValidateRewrite(ModuleTypes.CodecepticonModules.CSharp))
            {
                return false;
            }

            if (!ValidateMappingFiles())
            {
                return false;
            }

            if (!ValidateProjectSettings())
            {
                return false;
            }

            return true;
        }

        protected bool ValidateProjectSettings()
        {
            // Set the default compilation configuration.
            CommandLineData.CSharp.Compilation.Configuration = "Release";
            CommandLineData.CSharp.Compilation.Settings = new Dictionary<string, string>
            {
                // These two disable the generation of *.pdb files.
                { "DebugType", "none" },
                { "DebugSymbols", "false" }
            };

            if (CommandLineData.CSharp.Compilation.Build)
            {
                if (!String.IsNullOrEmpty(CommandLineData.CSharp.Compilation.OutputPath))
                {
                    CommandLineData.CSharp.Compilation.Settings.Add("OutputPath", CommandLineData.CSharp.Compilation.OutputPath);
                }
            }

            return true;
        }

        protected bool ProfileSpecificValidation()
        {
            if (CommandLineData.CSharp.Profile != null && !CommandLineData.CSharp.Profile.ValidateCommandLine())
            {
                Logger.Error("Command line did not meet profile requirements.");
                return false;
            }
            return true;
        }
    }
}
