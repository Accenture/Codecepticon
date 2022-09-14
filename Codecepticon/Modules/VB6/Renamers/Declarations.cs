using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Codecepticon.Modules.VB6.Renamers
{
    class Declarations : VisualBasic6BaseListener
    {
        public static SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        private TokenStreamRewriter Rewriter { get; }

        public Declarations(CommonTokenStream stream)
        {
            Rewriter = new TokenStreamRewriter(stream);
        }

        public string GetText()
        {
            return Rewriter.GetText();
        }

        public override void EnterDeclareStmt([NotNull] VisualBasic6Parser.DeclareStmtContext context)
        {
            if (!Helper.DeclarationHasAlias(context))
            {
                string newDeclaration = Helper.DeclarationAddAlias(context);
                Rewriter.Replace(context.Start, context.Stop, newDeclaration);
            }
            base.EnterDeclareStmt(context);
        }
    }
}
