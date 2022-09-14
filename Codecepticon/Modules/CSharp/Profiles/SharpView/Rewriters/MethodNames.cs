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
    class MethodNames : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (!node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return base.VisitLiteralExpression(node);
            }

            string text = node.Token.Value.ToString();
            if (DataCollector.Mapping.Functions.ContainsKey(text))
            {
                SyntaxNode nParent = Helper.FindParentOfType(node, SyntaxKind.SimpleAssignmentExpression);
                if (nParent != null)
                {
                    (string variable, string value) = Helper.GetAssignmentData(nParent);
                    if (variable == "methodName")
                    {
                        return SyntaxFactory.ParseExpression($"\"{DataCollector.Mapping.Functions[text]}\"");
                    }
                }
            }

            return base.VisitLiteralExpression(node);
        }
    }
}
