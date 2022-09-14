using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codecepticon.Modules.CSharp.CommandLine
{
    class OptionsParser
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public async Task<Dictionary<string, DataCollector.CommandLine>> ParseOptionsFile(Document document)
        {
            Dictionary<string, DataCollector.CommandLine> commandLineData = new Dictionary<string, DataCollector.CommandLine>();

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var properties = syntaxTree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var p in properties)
            {
                if (!Helper.IsOptionChild(p))
                {
                    continue;
                }

                DataCollector.CommandLine data = Helper.GetOptionData(p);
                if (String.IsNullOrEmpty(data.Argument))
                {
                    continue;
                }

                // Sometimes the property is different to the actual command line, so we need to add it manually. For example "CollectionMethod"'s property is "CollectionMethods".
                if (!DataCollector.Mapping.Properties.ContainsKey(data.Argument))
                {
                    DataCollector.Mapping.Properties.Add(data.Argument, CommandLineData.Global.NameGenerator.Generate());
                }
                commandLineData.Add(data.PropertyName, data);
            }

            return commandLineData;
        }
    }
}
