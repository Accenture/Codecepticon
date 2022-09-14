using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Utils;

namespace Codecepticon.Modules.VB6.CommandLine
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
            // Check if the solution exists.
            if (String.IsNullOrEmpty(CommandLineData.Global.Project.Path) || !File.Exists(CommandLineData.Global.Project.Path))
            {
                Logger.Error($"Input path is empty or does not exist: {CommandLineData.Global.Project.Path}");
                return false;
            }

            if (String.IsNullOrEmpty(CommandLineData.Global.Project.SaveAs))
            {
                // Default behaviour is to replace the actual file.
                CommandLineData.Global.Project.SaveAs = CommandLineData.Global.Project.Path + ".obfuscated.vba";
            }

            if (CommandLineData.Vb6.Rename.Enabled && !ValidateRename())
            {
                return false;
            }

            if (!ValidateRewrite(ModuleTypes.CodecepticonModules.Vb6))
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
