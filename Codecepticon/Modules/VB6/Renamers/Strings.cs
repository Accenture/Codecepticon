using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Codecepticon.CommandLine;
using Codecepticon.Utils;

namespace Codecepticon.Modules.VB6.Renamers
{
    class Strings : VisualBasic6BaseListener
    {
        public static SyntaxTreeHelper Helper = new SyntaxTreeHelper();
        private TokenStreamRewriter Rewriter { get; }

        protected NameGenerator NameGenerator = new NameGenerator(NameGenerator.RandomNameGeneratorMethods.RandomCombinations, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", 32);

        public Strings(CommonTokenStream stream)
        {
            Rewriter = new TokenStreamRewriter(stream);
        }

        public string GetText()
        {
            return Rewriter.GetText();
        }

        public override void EnterLiteral([NotNull] VisualBasic6Parser.LiteralContext context)
        {
            string text = context.GetText();
            if (text.Length > 2 && text.Substring(0, 1) == "\"")
            {
                // If length == 2 it could be an empty string, ie "". Also check if it's a constant.
                if (!Helper.IsStringConstant(context))
                {
                    Rewriter.Replace(context.Start, context.Stop, RewriteString(text.Trim('"')));
                }
            }
            base.EnterLiteral(context);
        }

        protected string RewriteString(string text)
        {
            string code = "";
            switch (CommandLineData.Global.Rewrite.EncodingMethod)
            {
                case StringEncoding.StringEncodingMethods.Base64:
                    text = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
                    code = CommandLineData.Global.Rewrite.Template.Function + "(\""+ text +"\")";
                    break;
                case StringEncoding.StringEncodingMethods.XorEncrypt:
                    string key = NameGenerator.Generate();
                    text = StringEncoding.Xor(text, key, false, false);
                    code = CommandLineData.Global.Rewrite.Template.Function + "(\"" + text + "\", \"" + key + "\")";
                    break;
                case StringEncoding.StringEncodingMethods.SingleCharacterSubstitution:
                    text = StringEncoding.SingleCharacter(text, CommandLineData.Global.Rewrite.SingleMapping);
                    code = CommandLineData.Global.Rewrite.Template.Function + "(\"" + text + "\")";
                    break;
                case StringEncoding.StringEncodingMethods.GroupCharacterSubstitution:
                    text = StringEncoding.GroupCharacter(text, CommandLineData.Global.Rewrite.GroupMapping);
                    code = CommandLineData.Global.Rewrite.Template.Function + "(\"" + text + "\")";
                    break;
                case StringEncoding.StringEncodingMethods.ExternalFile:
                    int index = StringEncoding.GetExternalFileIndex(text);
                    code = CommandLineData.Global.Rewrite.Template.Function + "(" + index + ")";
                    break;
            }

            return code;
        }
    }
}
