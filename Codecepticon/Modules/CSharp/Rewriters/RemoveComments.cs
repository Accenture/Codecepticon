using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Codecepticon.Modules.CSharp.Rewriters
{
    class RemoveComments : CSharpSyntaxRewriter
    {
        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia) || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            {
                return default(SyntaxTrivia);
            }
            return base.VisitTrivia(trivia);
        }
    }
}
