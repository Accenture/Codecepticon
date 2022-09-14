using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Utils;

namespace Codecepticon.Modules.CSharp.Rewriters
{
    class Strings : CSharpSyntaxRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        protected NameGenerator NameGenerator = new NameGenerator(NameGenerator.RandomNameGeneratorMethods.RandomCombinations, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", 32);

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (!node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return base.VisitLiteralExpression(node);
            }
            
            if (Helper.IsAssemblyAttribute(node))
            {
                // Assemblies are re-written in a separate step.
                return base.VisitLiteralExpression(node);
            }
            
            if (!Helper.CanReplaceString(node))
            {
                return base.VisitLiteralExpression(node);
            }

            return RewriteString(node.Token.Value.ToString());
        }

        public override SyntaxNode VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
        {
            // First check if the interpolation has a custom format, and if it does, skip it.
            if (Helper.HasInterpolationFormat(node))
            {
                return base.VisitInterpolatedStringExpression(node);
            }

            string format = "";
            string vars = "";
            int varCount = 0;

            foreach (SyntaxNode n in node.ChildNodes())
            {
                switch (n.Kind())
                {
                    case SyntaxKind.InterpolatedStringText:
                        format += n.ChildTokens().First().ValueText;
                        break;
                    case SyntaxKind.Interpolation:
                        if (vars.Length > 0) { vars += ","; }
                        vars += $"{n.ChildNodes().First().GetText()}";

                        format += $"{{{varCount}}}";
                        varCount++;

                        break;
                }
            }

            string encodedString = GetEncodedString(format);
            if (varCount > 0)
            {
                encodedString = $"System.String.Format({encodedString}, {vars})";
            }

            return SyntaxFactory.ParseExpression(encodedString);
        }

        protected ExpressionSyntax RewriteString(string text)
        {
            return SyntaxFactory.ParseExpression(GetEncodedString(text));
        }

        protected string GetEncodedString(string text)
        {
            string code = "";
            switch (CommandLineData.Global.Rewrite.EncodingMethod)
            {
                case StringEncoding.StringEncodingMethods.Base64:
                    text = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
                    code = $"System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(\"{text}\"))";
                    break;
                case StringEncoding.StringEncodingMethods.XorEncrypt:
                    string key = NameGenerator.Generate();
                    text = StringEncoding.Xor(text, key, false, false);
                    code = $"{CommandLineData.Global.Rewrite.Template.Namespace}.{CommandLineData.Global.Rewrite.Template.Class}.{CommandLineData.Global.Rewrite.Template.Function}(\"{text}\", \"{key}\")";
                    break;
                case StringEncoding.StringEncodingMethods.SingleCharacterSubstitution:
                    text = StringEncoding.SingleCharacter(text, CommandLineData.Global.Rewrite.SingleMapping);
                    code = $"{CommandLineData.Global.Rewrite.Template.Namespace}.{CommandLineData.Global.Rewrite.Template.Class}.{CommandLineData.Global.Rewrite.Template.Function}(\"{text}\")";
                    break;
                case StringEncoding.StringEncodingMethods.GroupCharacterSubstitution:
                    text = StringEncoding.GroupCharacter(text, CommandLineData.Global.Rewrite.GroupMapping);
                    code = $"{CommandLineData.Global.Rewrite.Template.Namespace}.{CommandLineData.Global.Rewrite.Template.Class}.{CommandLineData.Global.Rewrite.Template.Function}(\"{text}\")";
                    break;
                case StringEncoding.StringEncodingMethods.ExternalFile:
                    int index = StringEncoding.GetExternalFileIndex(text);
                    code = $"{CommandLineData.Global.Rewrite.Template.Namespace}.{CommandLineData.Global.Rewrite.Template.Class}.{CommandLineData.Global.Rewrite.Template.Function}({index})";
                    break;
            }

            return code;
        }
    }
}
