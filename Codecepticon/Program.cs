using Codecepticon.CommandLine;
using Codecepticon.Modules.CSharp;
using Codecepticon.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.Modules.PowerShell;
using Codecepticon.Modules.VB6;
using static Codecepticon.Modules.ModuleTypes;
using Newtonsoft.Json;
using Codecepticon.Modules.Sign;

namespace Codecepticon
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Logger.Info("Run --help for more, or use the Command Line Generator HTML file to generate a command", true, false);
                return;
            }

            CommandLineManager cmdManager = new CommandLineManager(args);
            if (cmdManager.IsHelp())
            {
                Logger.Info(cmdManager.LoadHelp(CodecepticonModules.None), true, false);
                return;
            }

            CodecepticonModules module = cmdManager.GetModule();
            if (module == CodecepticonModules.None || module == CodecepticonModules.Unknown)
            {
                Logger.Error("No module is defined or module is invalid.", true, false);
                return;
            }

            Logger.Info($"Codecepticon v{CommandLineData.Global.Version} is starting...");
            if (!cmdManager.LoadCommandLineArguments(module))
            {
                if (CommandLineData.Global.IsHelp)
                {
                    Logger.Info(cmdManager.LoadHelp(module), true, false);
                    return;
                }
                Logger.Error("Could not parse command line.");
                return;
            }

            Logger.Debug("Global Command Line Data");
            Logger.Debug(JsonConvert.SerializeObject(CommandLineData.Global));

            Logger.Debug("C# Command Line Data");
            Logger.Debug(JsonConvert.SerializeObject(CommandLineData.CSharp));

            Logger.Debug("VB6 Command Line Data");
            Logger.Debug(JsonConvert.SerializeObject(CommandLineData.Vb6));

            Logger.Debug("PowerShell Command Line Data");
            Logger.Debug(JsonConvert.SerializeObject(CommandLineData.PowerShell));

            switch (module)
            {
                case CodecepticonModules.CSharp:
                    CSharpManager CSharpManager = new CSharpManager();
                    await CSharpManager.Run();
                    break;
                case CodecepticonModules.Powershell:
                    PowerShellManager PowerShellManager = new PowerShellManager();
                    await PowerShellManager.Run();
                    break;
                case CodecepticonModules.Vb6:
                    Vb6Manager vb6Manager = new Vb6Manager();
                    await vb6Manager.Run();
                    break;
                case CodecepticonModules.Sign:
                    SignManager signManager = new SignManager();
                    await signManager.Run();
                    break;
                default:
                    Logger.Error("Code error: Module manager not implemented.");
                    return;
            }
        }
    }
}
