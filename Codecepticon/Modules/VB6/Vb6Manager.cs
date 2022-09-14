using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Codecepticon.CommandLine;
using Codecepticon.Utils;

namespace Codecepticon.Modules.VB6
{
    class Vb6Manager : ModuleManager
    {
        public struct ParsedFileStruct
        {
            public CommonTokenStream Stream;
            public IParseTree Tree;
            public ICharStream CharStream;
            public ITokenSource Lexer;
            public VisualBasic6Parser Parser;

            public ParsedFileStruct(string sourceCode)
            {
                CharStream = CharStreams.fromString(sourceCode);
                Lexer = new VisualBasic6Lexer(CharStream);
                Stream = new CommonTokenStream(Lexer);
                Parser = new VisualBasic6Parser(Stream);
                Tree = Parser.startRule();
            }

            public string GetSourceCode()
            {
                string output = Tree.GetText();
                if (output.Substring(output.Length - 5) == "<EOF>")
                {
                    output = output.Substring(0, output.Length - 5);
                }

                return output;
            }
        }

        public async Task Run()
        {
            switch (CommandLineData.Global.Action)
            {
                case CommandLineData.Action.Obfuscate:
                    await Obfuscate();
                    break;
                case CommandLineData.Action.Unmap:
                    await Unmap();
                    break;
            }
        }

        public static async Task Obfuscate()
        {
            Logger.Info("Reading file " + CommandLineData.Global.Project.Path);
            string sourceCode = File.ReadAllText(CommandLineData.Global.Project.Path);

            Logger.Info("Parsing file...");
            ParsedFileStruct parsedFile = new ParsedFileStruct(sourceCode);

            // First we need to rename any declarations that do not have an "Alias" keyword.
            Logger.Info("Rewriting bits and pieces...");
            parsedFile = await RewriteDeclarations(parsedFile);

            await GatherProjectData(parsedFile.Tree);

            Logger.Verbose("");
            Logger.Verbose("Elements Found:");
            Logger.Verbose($"\tIdentifiers:\t{DataCollector.AllIdentifiers.Count}");

            Logger.Info("Generating mappings...");
            if (await GenerateMappings() == false)
            {
                Logger.Error("Could not generate mappings.");
                return;
            }

            Logger.Info("Rewriting code...");
            parsedFile = await RewriteCode(parsedFile);

            switch (CommandLineData.Global.Rewrite.EncodingMethod)
            {
                case StringEncoding.StringEncodingMethods.ExternalFile:
                    StringEncoding.SaveExportExternalFileMapping(CommandLineData.Global.Rewrite.ExternalFile);
                    break;
            }

            File.WriteAllText(CommandLineData.Global.Project.SaveAs, parsedFile.GetSourceCode());

            Logger.Info("Generating mapping file to: " + CommandLineData.Global.Unmap.MapFile);
            Unmapping.GenerateMapFile(CommandLineData.Global.Unmap.MapFile);
            Logger.Success("Obfuscation complete");
        }

        public static async Task<ParsedFileStruct> RewriteDeclarations(ParsedFileStruct parsedFile)
        {
            DataRenamer dataRenamer = new DataRenamer();
            if (CommandLineData.Vb6.Rename.Identifiers)
            {
                parsedFile = await dataRenamer.RewriteDeclarations(parsedFile);
            }

            return new ParsedFileStruct(parsedFile.GetSourceCode());
        }

        public static async Task<ParsedFileStruct> RewriteCode(ParsedFileStruct parsedFile)
        {
            DataRenamer dataRenamer = new DataRenamer();

            string stringDecoding = "";
            if (CommandLineData.Global.Rewrite.Strings)
            {
                stringDecoding = dataRenamer.AddStringHelperFunction();
            }

            if (CommandLineData.Vb6.Rename.Identifiers)
            {
                parsedFile = await dataRenamer.RenameIdentifiers(parsedFile);
            }

            if (CommandLineData.Global.Rewrite.Strings)
            {
                parsedFile = await dataRenamer.RewriteStrings(parsedFile);
            }

            string sourceCode = parsedFile.GetSourceCode();
            if (stringDecoding.Length > 0)
            {
                sourceCode += "\r\n" + stringDecoding;
            }

            return new ParsedFileStruct(sourceCode);
        }

        public static async Task GatherProjectData(IParseTree tree)
        {
            DataCollector dataCollector = new DataCollector();
            dataCollector.CollectData(tree);
        }

        public static async Task<bool> GenerateMappings()
        {
            CommandLineData.Global.NameGenerator = new NameGenerator(
                CommandLineData.Global.RenameGenerator.Method,
                CommandLineData.Global.RenameGenerator.Data.CharacterSet.Value,
                CommandLineData.Global.RenameGenerator.Data.CharacterSet.Length,
                CommandLineData.Global.RenameGenerator.Data.Dictionary.Words
            );

            if (CommandLineData.Vb6.Rename.Identifiers)
            {
                Logger.Verbose("Creating mappings for identifiers");
                foreach (string name in DataCollector.AllIdentifiers)
                {
                    string newName = await GenerateName(CommandLineData.Global.NameGenerator, DataCollector.IsMappingUnique);
                    if (newName.Length == 0)
                    {
                        return false;
                    }

                    DataCollector.Mapping.Identifiers.Add(name, newName);
                }
            }

            return true;
        }

        public static async Task Unmap()
        {
            Unmapping unmapping = new Unmapping();
            unmapping.Run(CommandLineData.Global.Unmap.MapFile, CommandLineData.Global.Unmap.File, CommandLineData.Global.Unmap.Directory, CommandLineData.Global.Unmap.Recursive);
        }
    }
}
