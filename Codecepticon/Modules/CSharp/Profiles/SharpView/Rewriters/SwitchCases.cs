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
    class SwitchCases : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (!node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return base.VisitLiteralExpression(node);
            }

            if (node.Parent.IsKind(SyntaxKind.CaseSwitchLabel))
            {
                SyntaxNode nParent = Helper.FindParentOfType(node, SyntaxKind.SwitchSection);
                if (nParent != null)
                {
                    string methodName = Helper.GetMethodMappedName(nParent);
                    if (methodName.Length > 0)
                    {
                        return SyntaxFactory.ParseExpression($"\"{methodName.ToLower()}\"");
                    }
                }
            }

            return base.VisitLiteralExpression(node);
        }
    }
}
