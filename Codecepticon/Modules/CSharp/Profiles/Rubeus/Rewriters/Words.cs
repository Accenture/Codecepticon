using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codecepticon.Modules.CSharp.Profiles.Rubeus.Rewriters
{
    class Words : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        protected List<string> RewriteWords = new List<string>(new[] { "hashcat", "john" });

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            string text = node.GetFirstToken().ValueText.Trim();
            return RewriteWords.Contains(text) ? Helper.RewriteCommandLineArg(node, text, "") : base.VisitLiteralExpression(node);
        }
    }
}
