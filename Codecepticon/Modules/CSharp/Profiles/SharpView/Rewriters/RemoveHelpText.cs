using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codecepticon.Modules.CSharp.Profiles.SharpView.Rewriters
{
    class RemoveHelpText : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (!node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return base.VisitLiteralExpression(node);
            }

            SyntaxNode ifStatement = Helper.FindParentOfType(node, SyntaxKind.IfStatement);
            if (ifStatement != null)
            {
                SyntaxNode equalStatement = Helper.FindChildOfType(ifStatement, SyntaxKind.EqualsExpression);
                if (equalStatement != null)
                {
                    if (equalStatement.GetText().ToString() == "args.Length == 0")
                    {
                        return SyntaxFactory.ParseExpression($"\"\"");
                    }
                }
            }

            return base.VisitLiteralExpression(node);
        }
    }
}
