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
    class OutstandingProperties : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            /*
             * For some reason these aren't renamed in a few instances, such as:
             * var restrictedMemberOfSets = actions.Where(x => x.Target == GroupActionTarget.RestrictedMemberOf)
                    .Select(x => (x.TargetRid, x.TargetSid, x.TargetType)).GroupBy(x => x.TargetRid); <== This last one is not renamed automatically.
             */

            switch (node.Identifier.ValueText)
            {
                case "TargetRid":
                case "TargetSid":
                case "TargetType":
                    if (DataCollector.Mapping.Properties.ContainsKey(node.Identifier.ValueText))
                    {
                        return SyntaxFactory.IdentifierName(DataCollector.Mapping.Properties[node.Identifier.ValueText]);
                    }
                    break;
                case "computer_name":
                case "lan_group":
                    if (DataCollector.Mapping.Variables.ContainsKey(node.Identifier.ValueText))
                    {
                        return SyntaxFactory.IdentifierName(DataCollector.Mapping.Variables[node.Identifier.ValueText]);
                    }
                    break;
            }

            return base.VisitIdentifierName(node);
        }
    }
}
