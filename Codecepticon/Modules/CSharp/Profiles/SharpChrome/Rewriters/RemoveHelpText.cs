using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.CSharp.Profiles.SharpChrome.Rewriters
{
    class RemoveHelpText : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        protected List<string> HelpFunctions = new List<string>
        {
            "showlogo",
            "showusage"
        };

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            BlockSyntax block = RewriteBlock(node);
            return block ?? base.VisitBlock(node);
        }

        protected BlockSyntax RewriteBlock(BlockSyntax node)
        {
            SyntaxNode pMethod = Helper.FindParentOfType(node, SyntaxKind.MethodDeclaration);
            if (pMethod == null)
            {
                return null;
            }

            string functionName = pMethod.ChildTokens().LastOrDefault().ValueText.ToLower();
            if (String.IsNullOrEmpty(functionName))
            {
                return null;
            }

            if (!HelpFunctions.Contains(functionName))
            {
                return null;
            }
            return SyntaxFactory.Block(null);
        }
    }
}
