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

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            string text = node.GetFirstToken().ValueText.Trim();
            if (Helper.GetPropertyDeclaration(node) == "Command")
            {
                if (Helper.GetClassBaseList(node) == "CommandBase")
                {
                    return Helper.RewriteCommandLineArg(node, text, false);
                }
            }
            return base.VisitLiteralExpression(node);
        }
    }
}
