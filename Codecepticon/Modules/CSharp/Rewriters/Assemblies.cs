using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codecepticon.Modules.CSharp.Rewriters
{
    class Assemblies : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        protected Random Rnd = new Random();

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            return Helper.IsAssemblyAttribute(node) ? RewriteAssemblyInfo(node) : base.VisitLiteralExpression(node);
        }

        protected ExpressionSyntax RewriteAssemblyInfo(SyntaxNode node)
        {
            List<string> companies = new List<string>
            {
                "Microsoft",
                "Adobe",
                "Google",
                "Dell",
                "Intel",
                "Sony",
                "HP",
                "Cisco",
                "NVIDIA",
                "Broadcom",
                "Oracle",
                "AMD",
                "Vmware",
                "Autodesk",
                "Zoom",
                "CrowdStrike",
                "CarbonBlack",
                "McAfee",
                "Sophos",
                "Symantec",
                "Avast",
                "Avira",
                "Windows Defender",
                "Eset",
                "Kaspersky",
                "F-Secure",
                "Fireeye",
                "Ivanti",
                "Tanium"
            };
            string assemblyPropertyName = Helper.GetAssemblyPropertyName(node);
            string value = node.GetText().ToString();
            switch (assemblyPropertyName)
            {
                case "AssemblyCompany":
                    value = $"\"{companies[Rnd.Next(companies.Count)]}\"";
                    break;
                case "AssemblyCopyright":
                    int year = Rnd.Next(2014, DateTime.Now.Year);
                    value = $"\"Copyright {year}\"";
                    break;
                case "AssemblyTitle":
                case "AssemblyDescription":
                case "AssemblyProduct":
                case "AssemblyTrademark":
                    value = "\"\"";
                    break;
                case "AssemblyVersion":
                case "AssemblyFileVersion":
                    int major = Rnd.Next(1, 9);
                    int minor = Rnd.Next(0, 20);
                    int revision = Rnd.Next(0, 20);
                    int patch = Rnd.Next(1000, 3000);
                    value = $"\"{major}.{minor}.{revision}.{patch}\"";
                    break;
                case "Guid":
                    value = $"\"{CommandLineData.Global.Project.Guid.ToString().ToLower()}\"";
                    break;
            }
            return SyntaxFactory.ParseExpression(value);
        }
    }
}
