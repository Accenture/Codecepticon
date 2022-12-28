using Codecepticon.CommandLine;
using Codecepticon.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.Sign
{
    class SignManager : ModuleManager
    {
        public async Task Run()
        {
            switch (CommandLineData.Global.Action)
            {
                case CommandLineData.Action.GenerateCertificate:
                    GenerateCertificate();
                    break;
                case CommandLineData.Action.Sign:
                    if (!FindSignTool())
                    {
                        return;
                    }
                    SignExecutable();
                    break;
            }
        }

        protected bool GenerateCertificate()
        {
            CertificateManager certificateManager = new CertificateManager();
            Logger.Info("Generating certificate...");
            try
            {
                bool result = certificateManager.GenerateCertificate(CommandLineData.Sign.NewCertificate.Subject, CommandLineData.Sign.NewCertificate.Issuer, CommandLineData.Sign.NewCertificate.NotBefore, CommandLineData.Sign.NewCertificate.NotAfter, CommandLineData.Sign.NewCertificate.Password, CommandLineData.Sign.NewCertificate.PfxFile);
                if (!result)
                {
                    Logger.Error("Could not generate self-signed certificate");
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return false;
            }
            Logger.Info("Certificate generated");
            return true;
        }

        protected bool FindSignTool()
        {
            if (String.IsNullOrEmpty(CommandLineData.Sign.SignTool))
            {
                Logger.Info("No signtool.exe specified, will look for it now...");
                SignToolManager signToolManager = new SignToolManager();
                CommandLineData.Sign.SignTool = signToolManager.Find();
                if (String.IsNullOrEmpty(CommandLineData.Sign.SignTool) || !File.Exists(CommandLineData.Sign.SignTool))
                {
                    Logger.Error("Could not find signtool.exe");
                    return false;
                }
                Logger.Info("Found signtool.exe: " + CommandLineData.Sign.SignTool);
            }
            return File.Exists(CommandLineData.Sign.SignTool);
        }

        protected bool SignExecutable()
        {
            string stdOutput = "";
            string stdError = "";

            Logger.Info("Signing executable...");
            SignToolManager signToolManager = new SignToolManager();
            bool result = signToolManager.SignExecutable(CommandLineData.Sign.SignTool, CommandLineData.Global.Project.Path, CommandLineData.Sign.NewCertificate.PfxFile, CommandLineData.Sign.NewCertificate.Password, ref stdOutput, ref stdError);
            if (!result)
            {
                Logger.Error("There was an error while signing the file:");
                Logger.Error("", true, false);
                Logger.Error(stdError, true, false);
                return false;
            }

            Logger.Success("Executable signed");
            return true;
        }
    }
}
