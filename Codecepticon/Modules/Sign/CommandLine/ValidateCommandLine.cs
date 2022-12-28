using Codecepticon.CommandLine;
using Codecepticon.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.Sign.CommandLine
{
    class ValidateCommandLine : CommandLineValidator
    {
        public bool Run()
        {
            if (!ValidateParameters())
            {
                return false;
            }

            return true;
        }

        protected bool ValidateParameters()
        {
            switch (CommandLineData.Global.Action)
            {
                case CommandLineData.Action.GenerateCertificate:

                    if (String.IsNullOrEmpty(CommandLineData.Sign.NewCertificate.CN))
                    {
                        Logger.Error("Certificate CN is empty");
                        return false;
                    }

                    if (CommandLineData.Sign.NewCertificate.NotBefore == default)
                    {
                        Logger.Error("Certificate NotBefore is invalid");
                        return false;
                    }
                    else if (CommandLineData.Sign.NewCertificate.NotAfter == default)
                    {
                        Logger.Error("Certificate NotAfter is invalid");
                        return false;
                    }

                    if (String.IsNullOrEmpty(CommandLineData.Sign.NewCertificate.Password))
                    {
                        Logger.Error("Certificate Password is empty");
                        return false;
                    }

                    if (String.IsNullOrEmpty(CommandLineData.Sign.NewCertificate.PfxFile))
                    {
                        Logger.Error("Certificate Pfx path is empty");
                        return false;
                    }
                    else if (File.Exists(CommandLineData.Sign.NewCertificate.PfxFile) && !CommandLineData.Sign.NewCertificate.Overwrite)
                    {
                        Logger.Error("Certificate Pfx path already exists and --overwrite was not set");
                        return false;
                    }

                    Logger.Debug("New Certificate CN: " + CommandLineData.Sign.NewCertificate.CN);
                    Logger.Debug("New Certificate NotBefore: " + CommandLineData.Sign.NewCertificate.NotBefore);
                    Logger.Debug("New Certificate NotAfter: " + CommandLineData.Sign.NewCertificate.NotAfter);
                    Logger.Debug("New Certificate Password: " + CommandLineData.Sign.NewCertificate.Password);
                    Logger.Debug("New Certificate PfxFile: " + CommandLineData.Sign.NewCertificate.PfxFile);

                    break;
                case CommandLineData.Action.Sign:
                    if (String.IsNullOrEmpty(CommandLineData.Global.Project.Path) || !File.Exists(CommandLineData.Global.Project.Path))
                    {
                        Logger.Error("Target path is empty or does not exist: " + CommandLineData.Global.Project.Path);
                        return false;
                    }

                    if (String.IsNullOrEmpty(CommandLineData.Sign.NewCertificate.PfxFile) || !File.Exists(CommandLineData.Sign.NewCertificate.PfxFile))
                    {
                        Logger.Error("Certificate Pfx path is empty or does not exist: " + CommandLineData.Sign.NewCertificate.PfxFile);
                        return false;
                    }

                    if (String.IsNullOrEmpty(CommandLineData.Sign.NewCertificate.Password))
                    {
                        Logger.Error("Certificate Password is empty");
                        return false;
                    }

                    if (String.IsNullOrEmpty(CommandLineData.Sign.SignTool))
                    {
                        Logger.Info("SignTool path is empty - will try to find signtool.exe");
                    }
                    else if (!File.Exists(CommandLineData.Sign.SignTool))
                    {
                        Logger.Error("Path for signtool.exe does not exist: " + CommandLineData.Sign.SignTool);
                        return false;
                    }

                    // Validate the PFX Password.
                    CertificateManager certificateManager = new CertificateManager();
                    if (!certificateManager.CheckPfxPassword(CommandLineData.Sign.NewCertificate.PfxFile, CommandLineData.Sign.NewCertificate.Password))
                    {
                        Logger.Error("Invalid PFX file password");
                        return false;
                    }

                    break;
                default:
                    Logger.Error("Invalid action: " + CommandLineData.Global.Action.ToString());
                    return false;
            }
            return true;
        }
    }
}
