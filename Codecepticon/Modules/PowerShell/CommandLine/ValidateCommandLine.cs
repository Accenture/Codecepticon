using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Utils;

namespace Codecepticon.Modules.PowerShell.CommandLine
{
    class ValidateCommandLine : CommandLineValidator
    {
        public bool Run()
        {
            if (!ValidateAction())
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
            if (!CommandLineData.PowerShell.Rename.Enabled && !CommandLineData.Global.Rewrite.Strings)
            {
                Logger.Error("No rename or rewrite parameters set.");
                return false;
            }

            // Check if the solution exists.
            if (String.IsNullOrEmpty(CommandLineData.Global.Project.Path) || !File.Exists(CommandLineData.Global.Project.Path))
            {
                Logger.Error($"Path is empty or does not exist: {CommandLineData.Global.Project.Path}");
                return false;
            }

            if (String.IsNullOrEmpty(CommandLineData.Global.Project.SaveAs))
            {
                // Default behaviour is to replace the actual file.
                CommandLineData.Global.Project.SaveAs = CommandLineData.Global.Project.Path + ".obfuscated.ps1";
            }

            if (CommandLineData.PowerShell.Rename.Enabled && !ValidateRename())
            {
                return false;
            }

            if (!ValidateRewrite(ModuleTypes.CodecepticonModules.Powershell))
            {
                return false;
            }

            if (!ValidateMappingFiles())
            {
                return false;
            }

            return true;
        }
    }
}
