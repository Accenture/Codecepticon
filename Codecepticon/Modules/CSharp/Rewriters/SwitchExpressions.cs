using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.CSharp.Rewriters
{
    class SwitchExpressions : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public SemanticModel documentModel = null;

        public override SyntaxNode VisitSwitchExpression(SwitchExpressionSyntax node)
        {
            /*
             *  This is a switch expression, for example the following snippet from Certify:
             *  
             *  private string? ConvertGuidToName(string guid)
                {
                    return guid switch
                    {
                        "0e10c968-78fb-11d2-90d4-00c04f79dc55" => "Enrollment",
                        "a05b8cc2-17bc-4802-a710-e7c15ab866a2" => "AutoEnrollment",
                        "00000000-0000-0000-0000-000000000000" => "All",
                        _ => null
                    };
                }
             *  
             *  And needs to be rewritten in order to be able to obfuscate it:
             *  
             *  if (guid == "0e10c968-78fb-11d2-90d4-00c04f79dc55") {
             *      return "Enrollment"
             *  } else if (guid == "a05b8cc2-17bc-4802-a710-e7c15ab866a2") {
             *      return "AutoEnrollment"
             *  } else if (guid == "00000000-0000-0000-0000-000000000000") {
             *      return "All"
             *  } else {
             *      return null
             *  }
             *  
             *  However, as it's inline it's difficult to go back and start changing things,
             *  so we'll use an inline function like:
             *  
             *  return ((Func<string, string>)((guid) => { return guid; }))(guid);
             *  
             *  We need to collect the following:
             *      1. Name of the variable that is compared against values = "guid"
             *      2. Type of that variable, as we'll need to define it and pass it as a parameter = "string"
             *      3. Type of the value that is being returned, in this case it's the function type = "string"
             */

            // First we need to get the identifier name.
            SyntaxNode identifierNode = Helper.FindChildOfType(node, SyntaxKind.IdentifierName);
            if (identifierNode != null)
            {
                // Get #1
                string identifierName = identifierNode.GetFirstToken().ValueText;

                // Get #2
                // ISymbol identifierSymbol = documentModel.GetSymbolInfo(identifierNode).Symbol;
                string identifierType = documentModel.GetSymbolInfo(identifierNode).Symbol.ToString();

                // Get #3
                string expressionType = documentModel.GetTypeInfo(node).Type.ToString();

                // Collect all comparisons.
                Dictionary<string, string> expressions = new Dictionary<string, string>();
                foreach (SyntaxNode switchArm in node.ChildNodes())
                {
                    // Just to confirm we have the right node.
                    if (!switchArm.IsKind(SyntaxKind.SwitchExpressionArm))
                    {
                        continue;
                    }

                    string constantPattern = switchArm.ChildNodes().ElementAt(0).GetText().ToString().Trim();
                    string returnValue = switchArm.ChildNodes().ElementAt(1).GetText().ToString().Trim();
                    expressions.Add(constantPattern, returnValue);
                }

                // Build function.
                string sourceCodeIfStatement = "";
                foreach (KeyValuePair<string, string> comparison in expressions)
                {
                    if (sourceCodeIfStatement == "")
                    {
                        sourceCodeIfStatement += $"if ({identifierName} == {comparison.Key})" + " {\r\n";
                    }
                    else
                    {
                        if (comparison.Key == "_")
                        {
                            sourceCodeIfStatement += " else {\r\n";
                        }
                        else
                        {
                            sourceCodeIfStatement += $" else if ({identifierName} == {comparison.Key})" + " {\r\n";
                        }
                    }

                    sourceCodeIfStatement += $"return {comparison.Value};";

                    sourceCodeIfStatement += "}\r\n";
                }

                string code = $"((Func<{identifierType}, {expressionType}>)(({identifierName}) => ";
                code += "{ " + sourceCodeIfStatement + " }";
                code += $"))({identifierName});";

                return SyntaxFactory.ParseExpression(code);
            }

            return base.VisitSwitchExpression(node);
        }
    }
}
