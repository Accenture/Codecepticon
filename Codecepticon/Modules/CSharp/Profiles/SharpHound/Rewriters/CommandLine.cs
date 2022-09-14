using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codecepticon.Modules.CSharp.Profiles.SharpHound.Rewriters
{
    class CommandLine : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (Helper.IsOptionParent(node))
            {
                string value = node.GetFirstToken().ValueText;
                string newValue = GetReplacementString(value);
                if (newValue.Length > 0)
                {
                    return SyntaxFactory.ParseExpression($"\"{newValue}\"");
                }

                // We also need to replace the "Default" collection method, if Enums have been renamed.
                if (CommandLineData.CSharp.Rename.Enums && value == "Default")
                {
                    newValue = DataCollector.Mapping.Enums[value];
                    return SyntaxFactory.ParseExpression($"\"{newValue}\"");
                }
            }
            return base.VisitLiteralExpression(node);
        }

        protected string GetReplacementString(string text)
        {
            string output = "";
            foreach (KeyValuePair<string, DataCollector.CommandLine> mapping in DataCollector.Mapping.CommandLine)
            {
                if (text == mapping.Value.Argument)
                {
                    output = mapping.Value.NewName;
                    break;
                }
            }

            return output;
        }
    }
}
