using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Codecepticon.Modules.CSharp.Rewriters
{
    class SwitchStatements : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public override SyntaxNode VisitSwitchStatement(SwitchStatementSyntax node)
        {
            /*
             *  This is a typical switch statement.
             */
            SyntaxNode switchCondition = Helper.FindChildOfType(node, new[] { SyntaxKind.InvocationExpression, SyntaxKind.IdentifierName, SyntaxKind.SimpleMemberAccessExpression });
            if (switchCondition == null)
            {
                return base.VisitSwitchStatement(node);
            }

            string sourceCodeIfStatement = "";
            foreach (SyntaxNode switchSection in node.ChildNodes())
            {
                // Just to confirm we have the right node.
                if (!switchSection.IsKind(SyntaxKind.SwitchSection))
                {
                    continue;
                }
                
                if (Helper.HasChildOfType(switchSection, SyntaxKind.CasePatternSwitchLabel))
                {
                    sourceCodeIfStatement = "";
                    break;
                }

                string ifLeft = switchCondition.GetText().ToString().Trim();

                List<string> conditions = Helper.GetSwitchCaseConditions(switchSection);
                string ifCondition = BuildIfCondition(conditions, ifLeft);

                if (sourceCodeIfStatement == "")
                {
                    sourceCodeIfStatement += $"if ({ifCondition})" + " {\r\n";
                }
                else
                {
                    if (ifCondition == "")
                    {
                        sourceCodeIfStatement += " else {\r\n";
                    }
                    else
                    {
                        sourceCodeIfStatement += $" else if ({ifCondition})" + " {\r\n";
                    }
                }

                sourceCodeIfStatement += Helper.GetSwitchCaseSourceCode(switchSection);

                sourceCodeIfStatement += "\r\n}";
            }

            return sourceCodeIfStatement != "" ? SyntaxFactory.ParseStatement(sourceCodeIfStatement) : base.VisitSwitchStatement(node);
        }

        protected string BuildIfCondition(List<string> conditions, string ifLeft)
        {
            string output = "";
            foreach (string condition in conditions)
            {
                if (output != "")
                {
                    output += " || ";

                }
                output += ifLeft + " == " + condition;
            }
            return output;
        }
    }
}
