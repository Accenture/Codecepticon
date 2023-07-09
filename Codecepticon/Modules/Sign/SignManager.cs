using Codecepticon.CommandLine;
using Codecepticon.Modules.Sign.MsSign;
using Codecepticon.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

        protected bool SignExecutable()
        {
            Logger.Info("Loading certificate...");
            
            X509Certificate2 certificate;
            try
            {
                certificate = new(CommandLineData.Sign.NewCertificate.PfxFile, CommandLineData.Sign.NewCertificate.Password);
            } catch (Exception e)
            {
                Logger.Error("Could not load PFX file: " + CommandLineData.Sign.NewCertificate.PfxFile);
                Logger.Error(e.Message);
                return false;
            }

            try
            {
                Logger.Info("Signing executable...");
                SignFileRequest request = new()
                {
                    Certificate = certificate,
                    PrivateKey = certificate.GetRSAPrivateKey(),
                    OverwriteSignature = true,
                    InputFilePath = CommandLineData.Global.Project.Path,
                    HashAlgorithm = CommandLineData.Sign.SignatureAlgorithm,
                    TimestampServer = CommandLineData.Sign.TimestampServer,
                };

                PortableExecutableSigningTool signingTool = new();
                SignFileResponse response = signingTool.SignFile(request);
                if (response.Status != SignFileResponseStatus.FileSigned && response.Status != SignFileResponseStatus.FileResigned)
                {
                    return false;
                }
            } catch (Exception e)
            {
                Logger.Error("Could not sign executable");
                Logger.Error(e.Message);
                return false;
            }
            

            Logger.Success("Executable signed");
            return true;
        }
    }
}
