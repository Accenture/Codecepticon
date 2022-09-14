using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Modules.CSharp.Rewriters;
using Codecepticon.Utils;
using Microsoft.CodeAnalysis.Text;

namespace Codecepticon.Modules.CSharp
{
    class DataRewriter
    {
        protected SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public async Task<Solution> RemoveComments(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync();
            RemoveComments rewriter = new RemoveComments();
            SyntaxNode newSyntaxRoot = rewriter.Visit(syntaxRoot);

            return solution.WithDocumentSyntaxRoot(document.Id, newSyntaxRoot);
        }

        public async Task<Solution> RewriteSwitchStatements(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync();
            SwitchStatements switchRewriter = new SwitchStatements();
            SyntaxNode newSyntaxRoot = switchRewriter.Visit(syntaxRoot);

            return solution.WithDocumentSyntaxRoot(document.Id, newSyntaxRoot);
        }

        public async Task<Solution> RewriteStrings(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync();
            Strings rewriter = new Strings();
            SyntaxNode newSyntaxRoot = rewriter.Visit(syntaxRoot);

            // Add required using statements.
            newSyntaxRoot = Helper.AddUsingStatement(newSyntaxRoot, "System");
            newSyntaxRoot = Helper.AddUsingStatement(newSyntaxRoot, "System.Linq");

            return solution.WithDocumentSyntaxRoot(document.Id, newSyntaxRoot);
        }

        public async Task<Solution> RewriteAssemblies(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync();
            Assemblies rewriter = new Assemblies();
            SyntaxNode newSyntaxRoot = rewriter.Visit(syntaxRoot);

            return solution.WithDocumentSyntaxRoot(document.Id, newSyntaxRoot);
        }

        public async Task<Solution> AddStringHelperClass(Solution solution, Project project)
        {
            string code = File.ReadAllText(CommandLineData.Global.Rewrite.Template.File);
            string mapping;

            // Find all variables that look like $_NAME_%
            Regex regex = new Regex(@"(%_[A-Za-z0-9_]+_%)");
            var matches = regex.Matches(code).Cast<Match>().Select(m => m.Value).ToArray().Distinct();

            // And now replace them all. We only need to keep track of the Namespace, Class, and Function names.
            foreach (var match in matches)
            {
                string name = CommandLineData.Global.NameGenerator.Generate();
                switch (match.ToLower())
                {
                    case "%_namespace_%":
                        Logger.Debug($"StringHelperClass - Replace %_namespace_% with {name}");
                        CommandLineData.Global.Rewrite.Template.Namespace = name;
                        break;
                    case "%_class_%":
                        Logger.Debug($"StringHelperClass - Replace %_class_% with {name}");
                        CommandLineData.Global.Rewrite.Template.Class = name;
                        break;
                    case "%_function_%":
                        Logger.Debug($"StringHelperClass - Replace %_function_% with {name}");
                        CommandLineData.Global.Rewrite.Template.Function = name;
                        break;
                }

                code = code.Replace(match, name);
            }

            switch (CommandLineData.Global.Rewrite.EncodingMethod)
            {
                case StringEncoding.StringEncodingMethods.SingleCharacterSubstitution:
                    mapping = StringEncoding.ExportSingleCharacterMap(CommandLineData.Global.Rewrite.SingleMapping, ModuleTypes.CodecepticonModules.CSharp);
                    code = code.Replace("%MAPPING%", mapping);
                    break;
                case StringEncoding.StringEncodingMethods.GroupCharacterSubstitution:
                    mapping = StringEncoding.ExportGroupCharacterMap(CommandLineData.Global.Rewrite.GroupMapping, ModuleTypes.CodecepticonModules.CSharp);
                    code = code.Replace("%MAPPING%", mapping);
                    break;
                case StringEncoding.StringEncodingMethods.ExternalFile:
                    code = code.Replace("%MAPPING%", Path.GetFileName(CommandLineData.Global.Rewrite.ExternalFile));
                    break;
            }

            SourceText source = SourceText.From(code);
            CommandLineData.Global.Rewrite.Template.AddedFile = GenerateStringFileName(project);
            Logger.Debug($"File added into project for strings: {CommandLineData.Global.Rewrite.Template.AddedFile}");
            Document document = project.AddDocument(CommandLineData.Global.Rewrite.Template.AddedFile, source);
            return solution.AddDocument(document.Id, CommandLineData.Global.Rewrite.Template.AddedFile, source);
        }

        protected string GenerateStringFileName(Project project)
        {
            // Get all the filenames.
            List<string> allFilenames = new List<string>();
            foreach (Document document in project.Documents)
            {
                allFilenames.Add(document.Name.Replace(".cs", ""));
            }

            // Randomly combine 2 names at a time until we get a unique name.
            Random rnd = new Random();
            string fileName;
            do
            {
                fileName = allFilenames[rnd.Next(allFilenames.Count)] + allFilenames[rnd.Next(allFilenames.Count)];
                if (allFilenames.Contains(fileName))
                {
                    fileName = "";
                }
            } while (fileName == "");

            return fileName + ".cs";
        }
    }
}
