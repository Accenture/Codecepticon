using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codecepticon.Modules.CSharp.Profiles.Rubeus.Rewriters
{
    class CommandLine : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            string text = node.GetFirstToken().ValueText.Trim();
            if (text.Length > 0 && text[0] == '/')
            {
                return Helper.RewriteCommandLineArg(node, text, "/");
            } 
            
            if (Helper.GetPropertyDeclaration(node) == "CommandName")
            {
                return Helper.RewriteCommandLineArg(node, text, "");
            }

            return base.VisitLiteralExpression(node);
        }
    }
}
