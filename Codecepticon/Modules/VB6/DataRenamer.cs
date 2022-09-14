using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Codecepticon.Modules.VB6.Renamers;
using Codecepticon.CommandLine;
using Codecepticon.Utils;

namespace Codecepticon.Modules.VB6
{
    class DataRenamer
    {
        protected NameGenerator NameGenerator = new NameGenerator(NameGenerator.RandomNameGeneratorMethods.RandomCombinations, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", 32);

        public async Task<Vb6Manager.ParsedFileStruct> RenameIdentifiers(Vb6Manager.ParsedFileStruct parsedFile)
        {
            Identifiers renameIdentifiers = new Identifiers(parsedFile.Stream);
            ParseTreeWalker.Default.Walk(renameIdentifiers, parsedFile.Tree);

            return new Vb6Manager.ParsedFileStruct(renameIdentifiers.GetText());
        }

        public async Task<Vb6Manager.ParsedFileStruct> RewriteStrings(Vb6Manager.ParsedFileStruct parsedFile)
        {
            Strings rewriteStrings = new Strings(parsedFile.Stream);
            ParseTreeWalker.Default.Walk(rewriteStrings, parsedFile.Tree);

            return new Vb6Manager.ParsedFileStruct(rewriteStrings.GetText());
        }

        public async Task<Vb6Manager.ParsedFileStruct> RewriteDeclarations(Vb6Manager.ParsedFileStruct parsedFile)
        {
            Declarations rewriteDeclarations = new Declarations(parsedFile.Stream);
            ParseTreeWalker.Default.Walk(rewriteDeclarations, parsedFile.Tree);

            return new Vb6Manager.ParsedFileStruct(rewriteDeclarations.GetText());
        }

        public string AddStringHelperFunction()
        {
            string code = File.ReadAllText(CommandLineData.Global.Rewrite.Template.File);
            string mapping;

            // Find all variables that look like %_NAME_%
            Regex regex = new Regex(@"(%_[A-Za-z0-9_]+_%)");
            var matches = regex.Matches(code).Cast<Match>().Select(m => m.Value).ToArray().Distinct();

            // And now replace them all. We only need to keep track of the Namespace, Class, and Function names.
            foreach (var match in matches)
            {
                string name = NameGenerator.Generate();
                switch (match.ToLower())
                {
                    case "%_function_%":
                        Logger.Debug($"StringHelperClass - Replace %_function_% with {name}");
                        CommandLineData.Global.Rewrite.Template.Function = name;
                        break;
                    case "%_mapping_var_%":
                        Logger.Debug($"StringHelperClass - Replace %_mapping_var_% with {name}");
                        CommandLineData.Global.Rewrite.Template.Mapping = name;
                        break;
                }

                code = code.Replace(match, name);
            }

            switch (CommandLineData.Global.Rewrite.EncodingMethod)
            {
                case StringEncoding.StringEncodingMethods.SingleCharacterSubstitution:
                    mapping = StringEncoding.ExportSingleCharacterMap(CommandLineData.Global.Rewrite.SingleMapping, ModuleTypes.CodecepticonModules.Vb6);
                    code = code.Replace("%MAPPING%", mapping);
                    break;
                case StringEncoding.StringEncodingMethods.GroupCharacterSubstitution:
                    mapping = StringEncoding.ExportGroupCharacterMap(CommandLineData.Global.Rewrite.GroupMapping, ModuleTypes.CodecepticonModules.Vb6);
                    code = code.Replace("%MAPPING%", mapping);
                    break;
                case StringEncoding.StringEncodingMethods.ExternalFile:
                    code = code.Replace("%MAPPING%", Path.GetFileName(CommandLineData.Global.Rewrite.ExternalFile));
                    break;
            }

            return code;
        }
    }
}
