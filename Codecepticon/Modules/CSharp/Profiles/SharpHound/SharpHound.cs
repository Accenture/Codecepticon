using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Microsoft.CodeAnalysis;
using Codecepticon.Modules.CSharp.CommandLine;
using Codecepticon.Modules.CSharp.Profiles.SharpHound.Rewriters;
using Codecepticon.Utils;
using Document = Microsoft.CodeAnalysis.Document;
using BuildEvaluation = Microsoft.Build.Evaluation;


namespace Codecepticon.Modules.CSharp.Profiles.SharpHound
{
    class SharpHound : BaseProfile
    {
        public override string Name { get; } = "SharpHound";
        public override async Task<Solution> Before(Solution solution, Project project)
        {
            Logger.Debug("SharpHound: Removing help text");
            solution = await RemoveHelpText(solution, project);
            
            if (CommandLineData.Global.Rewrite.Strings)
            {
                Logger.Debug("SharpHound: Rewriting 'Is' statements");
                /*
                 * The way the following line is written prevents us from rewriting the string as it doesn't accept function calls.
                 * https://github.com/BloodHoundAD/SharpHound/blob/eca8b7d6c84b8610dfc26ba7a5b652beed70b0a7/src/BaseContext.cs#L105
                 *
                 * So we're just going to rewrite it as an 'if' statement.
                 */
                solution = await FixIsStatements(solution, project);
            }

            return solution;
        }

        public override async Task<Solution> After(Solution solution, Project project)
        {
            BuildEvaluation.Project buildProject = VisualStudioManager.GetBuildProject(project);

            if (CommandLineData.CSharp.Rename.Properties || CommandLineData.CSharp.Rename.Variables)
            {
                Logger.Debug("SharpHound: Fixing outstanding properties");
                solution = await FixOutstandingProperties(solution, project);
            }

            if (CommandLineData.CSharp.Rename.CommandLine)
            {
                Logger.Debug("SharpHound: Renaming command line");
                solution = await RewriteCommandLine(solution, project);
            }
            
            if (CommandLineData.CSharp.Rename.Namespaces)
            {
                Logger.Debug("SharpHound: Renaming RootNamespace");
                string rootNamespace = buildProject.GetPropertyValue("RootNamespace");
                string newNamespace = DataCollector.Mapping.Namespaces[rootNamespace];
                buildProject.SetProperty("RootNamespace", newNamespace);
            }

            Logger.Debug("SharpHound: Setting Misc Properties");
            buildProject.SetProperty("AssemblyName", CommandLineData.Global.NameGenerator.Generate());
            buildProject.SetProperty("Company", CommandLineData.Global.NameGenerator.Generate());
            buildProject.SetProperty("Product", CommandLineData.Global.NameGenerator.Generate());
            buildProject.Save();
            return solution;
        }

        public override async Task<Solution> Final(Solution solution, Project project)
        {
            BuildEvaluation.Project buildProject = VisualStudioManager.GetBuildProject(project);

            if (CommandLineData.Global.Rewrite.Strings)
            {
                /*
                 * "What is this?" I hear you ask... Here you go - https://github.com/dotnet/roslyn/issues/36781
                 * We need to remove the last reference to the file we added for decoding strings.
                 */
                BuildEvaluation.ProjectItem addedItem = buildProject.GetItems("Compile").LastOrDefault(item => item.EvaluatedInclude.Equals(CommandLineData.Global.Rewrite.Template.AddedFile));
                if (addedItem != null)
                {
                    Logger.Debug("SharpHound: Removing strings file from 'Compile' properties");
                    buildProject.RemoveItem(addedItem);
                }
            }

            buildProject.Save();
            return solution;
        }

        public override bool ValidateCommandLine()
        {
            if (CommandLineData.CSharp.Rename.CommandLine && !CommandLineData.CSharp.Rename.Properties)
            {
                Logger.Error("SharpHound Profile Error: In order to obfuscate the command line, you will also need to obfuscate the properties. Please update your arguments and try again.");
                return false;
            }
            return true;
        }

        protected async Task<Solution> RewriteCommandLine(Solution solution, Project project)
        {
            Dictionary<string, DataCollector.CommandLine> data = new Dictionary<string, DataCollector.CommandLine>();
            foreach (KeyValuePair<string, DataCollector.CommandLine> mapping in DataCollector.Mapping.CommandLine)
            {
                if (DataCollector.Mapping.Properties.ContainsKey(mapping.Key))
                {
                    DataCollector.CommandLine item = DataCollector.Mapping.CommandLine[mapping.Key];
                    item.NewName = DataCollector.Mapping.Properties[mapping.Key];
                    data.Add(mapping.Key, item);
                }
            }

            DataCollector.Mapping.CommandLine = data;

            // Now we need to rewrite the remaining arguments that are defined by a hardcoded value rather than the property name (ie "CollectionMethod").
            project = VisualStudioManager.GetProjectByName(solution, project.Name);
            Document csOptions = VisualStudioManager.GetDocumentByName(project, "Options.cs");

            SyntaxNode syntaxRoot = await csOptions.GetSyntaxRootAsync();
            Rewriters.CommandLine rewriter = new Rewriters.CommandLine();
            SyntaxNode newSyntaxRoot = rewriter.Visit(syntaxRoot);
            solution = solution.WithDocumentSyntaxRoot(csOptions.Id, newSyntaxRoot);

            return solution;
        }

        protected async Task<Solution> FixOutstandingProperties(Solution solution, Project project)
        {
            project = VisualStudioManager.GetProjectByName(solution, project.Name);

            foreach (Document document in project.Documents)
            {
                SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync();
                OutstandingProperties rewriter = new OutstandingProperties();
                SyntaxNode newSyntaxRoot = rewriter.Visit(syntaxRoot);
                solution = solution.WithDocumentSyntaxRoot(document.Id, newSyntaxRoot);
            }

            return solution;
        }

        protected async Task<Solution> RemoveHelpText(Solution solution, Project project)
        {
            OptionsParser parser = new OptionsParser();

            project = VisualStudioManager.GetProjectByName(solution, project.Name);
            Document csOptions = VisualStudioManager.GetDocumentByName(project, "Options.cs");
            
            DataCollector.Mapping.CommandLine = await parser.ParseOptionsFile(csOptions);
            
            SyntaxNode syntaxRoot = await csOptions.GetSyntaxRootAsync();
            RemoveHelpText rewriter = new RemoveHelpText();
            SyntaxNode newSyntaxRoot = rewriter.Visit(syntaxRoot);

            solution = solution.WithDocumentSyntaxRoot(csOptions.Id, newSyntaxRoot);

            return solution;
        }

        protected async Task<Solution> FixIsStatements(Solution solution, Project project)
        {
            project = VisualStudioManager.GetProjectByName(solution, project.Name);

            foreach (Document document in project.Documents)
            {
                SyntaxNode syntaxRoot = await document.GetSyntaxRootAsync();
                IsStatements rewriter = new IsStatements();
                SyntaxNode newSyntaxRoot = rewriter.Visit(syntaxRoot);
                solution = solution.WithDocumentSyntaxRoot(document.Id, newSyntaxRoot);
            }

            return solution;
        }
    }
}
