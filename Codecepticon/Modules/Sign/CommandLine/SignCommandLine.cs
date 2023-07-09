using Codecepticon.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.Sign.CommandLine
{
    class SignCommandLine : CommandLineManager
    {
        public SignCommandLine(string[] args) : base(args)
        {
            Arguments = new Dictionary<string, string>
            {
                { "issuer", "" },
                { "subject", "" },
                { "copy-from", "" },
                { "not-before", "" },
                { "not-after", "" },
                { "password", "" },
                { "pfx-file", "" },
                { "overwrite", "switch" },
                { "algorithm", "" },
                { "timestamp", "" }
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
                    case "subject":
                        CommandLineData.Sign.NewCertificate.Subject = argument.Value;
                        break;
                    case "issuer":
                        CommandLineData.Sign.NewCertificate.Issuer = argument.Value;
                        break;
                    case "copy-from":
                        CommandLineData.Sign.NewCertificate.CopyFrom = argument.Value;
                        break;
                    case "not-after":
                        try
                        {
                            CommandLineData.Sign.NewCertificate.NotAfter = DateTime.ParseExact(argument.Value, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (Exception e)
                        {
                            // Nothing.
                        }
                        break;
                    case "not-before":
                        try
                        {
                            CommandLineData.Sign.NewCertificate.NotBefore = DateTime.ParseExact(argument.Value, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (Exception e)
                        {
                            // Nothing.
                        }
                        break;
                    case "password":
                        CommandLineData.Sign.NewCertificate.Password = argument.Value;
                        break;
                    case "pfx-file":
                        CommandLineData.Sign.NewCertificate.PfxFile = argument.Value;
                        break;
                    case "overwrite":
                        if (argument.Value.ToLower() != "false")
                        {
                            CommandLineData.Sign.NewCertificate.Overwrite = (argument.Value.Length > 0);
                        }
                        break;
                    case "algorithm":
                        CommandLineData.Sign.SignatureAlgorithm = argument.Value.ToUpper();
                        break;
                    case "timestamp":
                        CommandLineData.Sign.TimestampServer = argument.Value;
                        break;
                }
            }

            ValidateCommandLine validate = new ValidateCommandLine();
            return validate.Run();
        }
    }
}
