using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codecepticon.Modules.CSharp.Profiles.Seatbelt.Rewriters
{
    class CommandLine : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        protected List<string> SeatbeltArgumentParsers = new List<string>
        {
            "ParseAndRemoveSwitchArgument",
            "ParseAndRemoveKeyValueArgument"
        };

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            string text = node.GetFirstToken().ValueText.Trim();
            if (Helper.GetPropertyDeclaration(node) == "Command")
            {
                if (Helper.GetClassBaseList(node) == "CommandBase")
                {
                    return Helper.RewriteCommandLineArg(node, text, "");
                }
            }

            /*
             * This will parse the following:
             *      var commandGroups = ParseAndRemoveKeyValueArgument("-Group");
             */
            if (text.Length > 0 && text.StartsWith("-"))
            {
                string functionName = Helper.GetCallingFunctionNameFromArgument(node);
                if (functionName.Length > 0 && SeatbeltArgumentParsers.Contains(functionName))
                {
                    return Helper.RewriteCommandLineArg(node, text, "-");
                }
            }
            
            return base.VisitLiteralExpression(node);
        }
    }
}
