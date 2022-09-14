using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codecepticon.Modules.CSharp.Profiles.SharpHound.Rewriters
{
    class IsStatements : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public override SyntaxNode VisitIsPatternExpression(IsPatternExpressionSyntax node)
        {
            string ifStatement = RewriteIsStatement(node);
            if (ifStatement.Length > 0)
            {
                return SyntaxFactory.ParseExpression(ifStatement);
            }
            return base.VisitIsPatternExpression(node);
        }

        protected string RewriteIsStatement(SyntaxNode node)
        {
            SyntaxNode nodeIdentifier = Helper.FindChildOfType(node, SyntaxKind.IdentifierName);
            if (nodeIdentifier == null)
            {
                return "";
            }

            SyntaxNode nodeOrPattern = Helper.FindChildOfType(node, SyntaxKind.OrPattern);
            if (nodeOrPattern == null)
            {
                return "";
            }

            List<SyntaxNode> nodeConstants = Helper.FindChildrenOfType(nodeOrPattern, SyntaxKind.ConstantPattern);
            if (nodeConstants.Count == 0)
            {
                return "";
            }

            string ifStatement = "";
            foreach (SyntaxNode pNode in nodeConstants)
            {
                if (ifStatement.Length > 0)
                {
                    ifStatement += " || ";
                }
                ifStatement += $"{nodeIdentifier.GetText()} == {pNode.GetText()}";
            }

            return $"({ifStatement})";
        }
    }
}
