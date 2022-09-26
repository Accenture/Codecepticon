using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.CSharp.Profiles.Seatbelt.Rewriters
{
    class CommandLine2 : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            string text = node.GetFirstToken().ValueText.Trim();
            if (text == "all")
            {
                return SyntaxFactory.ParseExpression($"\"{DataCollector.Mapping.Enums["All"].ToLower()}\"");
            }
            return base.VisitLiteralExpression(node);
        }
    }
}
