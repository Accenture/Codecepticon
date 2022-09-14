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
    class RemoveHelpText : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (Helper.IsOptionParent(node))
            {
                string argName = Helper.GetArgumentName(node);
                if (argName == "HelpText")
                {
                    return SyntaxFactory.ParseExpression("\"\"");
                }
            }
            return base.VisitLiteralExpression(node);
        }
    }
}
