using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Codecepticon.Modules.VB6.Renamers
{
    class Identifiers : VisualBasic6BaseListener
    {
        private TokenStreamRewriter Rewriter { get; }

        public Identifiers(CommonTokenStream stream)
        {
            Rewriter = new TokenStreamRewriter(stream);
        }

        public string GetText()
        {
            return Rewriter.GetText();
        }

        public override void EnterAmbiguousIdentifier(VisualBasic6Parser.AmbiguousIdentifierContext context)
        {
            string text = context.GetText();
            if (DataCollector.Mapping.Identifiers.ContainsKey(text))
            {
                Rewriter.Replace(context.Start, context.Stop, DataCollector.Mapping.Identifiers[text]);
            }
            base.EnterAmbiguousIdentifier(context);
        }

        public override void EnterCertainIdentifier([NotNull] VisualBasic6Parser.CertainIdentifierContext context)
        {
            string text = context.GetText();
            if (DataCollector.Mapping.Identifiers.ContainsKey(text))
            {
                Rewriter.Replace(context.Start, context.Stop, DataCollector.Mapping.Identifiers[text]);
            }
            base.EnterCertainIdentifier(context);
        }
    }
}
