using Codecepticon.CommandLine;
using Codecepticon.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
            CertificateManager certificateManager = new CertificateManager();

            switch (CommandLineData.Global.Action)
            {
                case CommandLineData.Action.GenerateCertificate:

                    string copyIssuer = "";
                    string copySubject = "";

                    if (!String.IsNullOrEmpty(CommandLineData.Sign.NewCertificate.CopyFrom))
                    {
                        if (!File.Exists(CommandLineData.Sign.NewCertificate.CopyFrom))
                        {
                            Logger.Error("File to copy certificate details from, does not exist: " + CommandLineData.Sign.NewCertificate.CopyFrom);
                            return false;
                        }

                        try
                        {
                            X509Certificate copyFromCertificate = certificateManager.GetCertificateFromFile(CommandLineData.Sign.NewCertificate.CopyFrom);
                            copyIssuer = copyFromCertificate.Issuer;
                            copySubject = copyFromCertificate.Subject;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Could not read the certificate from: " + CommandLineData.Sign.NewCertificate.CopyFrom + " - Are you sure it's signed?");
                            return false;
                        }
                    }
                    

                    if (String.IsNullOrEmpty(CommandLineData.Sign.NewCertificate.Subject))
                    {
                        if (String.IsNullOrEmpty(copySubject))
                        {
                            Logger.Error("Certificate Subject is empty");
                            return false;
                        }
                        CommandLineData.Sign.NewCertificate.Subject = copySubject;
                    }

                    if (String.IsNullOrEmpty(CommandLineData.Sign.NewCertificate.Issuer))
                    {
                        if (String.IsNullOrEmpty(copyIssuer))
                        {
                            Logger.Error("Certificate Issuer is empty");
                            return false;
                        }
                        CommandLineData.Sign.NewCertificate.Issuer = copyIssuer;
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

                    CommandLineData.Sign.NewCertificate.Subject = FixDN(CommandLineData.Sign.NewCertificate.Subject);
                    CommandLineData.Sign.NewCertificate.Issuer = FixDN(CommandLineData.Sign.NewCertificate.Issuer);

                    Logger.Debug("New Certificate Subject: " + CommandLineData.Sign.NewCertificate.Subject);
                    Logger.Debug("New Certificate Issuer: " + CommandLineData.Sign.NewCertificate.Issuer);
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

                    // Validate the PFX Password.
                    if (!certificateManager.CheckPfxPassword(CommandLineData.Sign.NewCertificate.PfxFile, CommandLineData.Sign.NewCertificate.Password))
                    {
                        Logger.Error("Invalid PFX file password");
                        return false;
                    }

                    if (String.IsNullOrEmpty(CommandLineData.Sign.SignatureAlgorithm))
                    {
                        Logger.Error("Signature Algorithm not set");
                        return false;
                    }
                    else if (!IsValidSignatureAlgorithm(CommandLineData.Sign.SignatureAlgorithm))
                    {
                        Logger.Error("Invalid signature algorithm selected");
                        return false;
                    }

                    if (String.IsNullOrEmpty(CommandLineData.Sign.TimestampServer))
                    {
                        CommandLineData.Sign.TimestampServer = ""; // Make sure it's not null.
                    }

                    break;
                default:
                    Logger.Error("Invalid action: " + CommandLineData.Global.Action.ToString());
                    return false;
            }
            return true;
        }

        protected bool IsValidSignatureAlgorithm(string algorithm)
        {
            List<string> validAlgorithms = new() { "MD5", "SHA1", "SHA256", "SHA384", "SHA512" };
            return validAlgorithms.Contains(algorithm);
        }

        protected string FixDN(string dn)
        {
            // BouncyCastle does not recognise S=XXX within an X509Name, and it has to be in the form of ST=XXX.
            // This function tries to convert the S= to ST=.

            Dictionary<string, string> searchAndReplace = new Dictionary<string, string>
            {
                { ",S=", ",ST=" },
                { ", S=", ", ST=" }
            };

            foreach (KeyValuePair<string, string> item in searchAndReplace)
            {
                if (dn.IndexOf(item.Key) < 0)
                {
                    continue;
                }
                dn = dn.Replace(item.Key, item.Value);
            }
            return dn;
        }
    }
}
