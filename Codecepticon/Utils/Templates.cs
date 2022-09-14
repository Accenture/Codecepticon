using Codecepticon.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Utils
{
    class Templates
    {
        protected Dictionary<StringEncoding.StringEncodingMethods, string> TemplateNames = new Dictionary<StringEncoding.StringEncodingMethods, string>
        {
            { StringEncoding.StringEncodingMethods.Base64, "Base64" },
            { StringEncoding.StringEncodingMethods.XorEncrypt, "XOR" },
            { StringEncoding.StringEncodingMethods.SingleCharacterSubstitution, "Single" },
            { StringEncoding.StringEncodingMethods.GroupCharacterSubstitution, "Group" },
            { StringEncoding.StringEncodingMethods.ExternalFile, "ExternalFile" }
        };

        protected Dictionary<ModuleTypes.CodecepticonModules, string> TemplateExtensions = new Dictionary<ModuleTypes.CodecepticonModules, string>
        {
            { ModuleTypes.CodecepticonModules.CSharp, "cs" },
            { ModuleTypes.CodecepticonModules.Powershell, "ps1" },
            { ModuleTypes.CodecepticonModules.Vb6, "vb6" },
        };

        public string GetTemplateFile(string fileName)
        {
            return Path.GetFullPath(@"Templates\" + fileName);
        }

        public string GetTemplateFile(ModuleTypes.CodecepticonModules module, StringEncoding.StringEncodingMethods method)
        {
            if (!TemplateNames.ContainsKey(method) || !TemplateExtensions.ContainsKey(module))
            {
                return "";
            }

            string fileName = TemplateNames[method] + "." + TemplateExtensions[module];
            fileName = Path.GetFullPath(@"Templates\" + fileName);
            return fileName;
        }
    }
}
