using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Modules.CSharp.Profiles.SharpView.Rewriters;
using Codecepticon.Utils;
using Microsoft.CodeAnalysis;

namespace Codecepticon.Modules.CSharp.Profiles.SharpView
{
    class SharpView : BaseProfile
    {
        public override string Name { get; } = "SharpView";

        public override async Task<Solution> Before(Solution solution, Project project)
        {
            if (CommandLineData.CSharp.Rename.CommandLine)
            {
                Logger.Debug("SharpView: Rewriting command line");
                solution = await RewriteCommandLine(solution, project);
            }

            // Remove Help usage (when running without any command line arguments).
            Logger.Debug("SharpView: Removing help text");
            solution = await RemoveHelpText(solution, project);
            return solution;
        }

        public override async Task<Solution> After(Solution solution, Project project)
        {
            Dictionary<string, string> projectConfiguration;

            if (CommandLineData.CSharp.Rename.Namespaces)
            {
                Logger.Debug("SharpView: Renaming project AssemblyName and RootNamespace");
                projectConfiguration = new Dictionary<string, string>
                {
                    { "AssemblyName", DataCollector.Mapping.Namespaces["SharpView"] },
                    { "RootNamespace", DataCollector.Mapping.Namespaces["SharpView"] },
                };

                VisualStudioManager.SetProjectConfiguration(solution, projectConfiguration);
            }

            if (CommandLineData.CSharp.Rename.Namespaces || CommandLineData.CSharp.Rename.Classes)
            {
                Logger.Debug("SharpView: Renaming StartupObject");
                string startUpObject = "";
                startUpObject += CommandLineData.CSharp.Rename.Namespaces ? DataCollector.Mapping.Namespaces["SharpView"] : "SharpView";
                startUpObject += ".";
                startUpObject += CommandLineData.CSharp.Rename.Classes ? DataCollector.Mapping.Classes["Program"] : "Program";

                projectConfiguration = new Dictionary<string, string>
                {
                    { "StartupObject", startUpObject }
                };

                VisualStudioManager.SetProjectConfiguration(solution, projectConfiguration);
            }

            return solution;
        }

        public override bool ValidateCommandLine()
        {
            if ((CommandLineData.CSharp.Rename.CommandLine && !CommandLineData.CSharp.Rename.Functions) || (!CommandLineData.CSharp.Rename.CommandLine && CommandLineData.CSharp.Rename.Functions))
            {
                Logger.Error("SharpView Profile Error: You cannot obfuscate the command line without obfuscating the functions, and vice versa. Please update your arguments to include both, and try again.");
                return false;
            }
            return true;
        }

        protected async Task<Solution> RewriteCommandLine(Solution solution, Project project)
        {
            project = VisualStudioManager.GetProjectByName(solution, project.Name);
            Document csProgram = VisualStudioManager.GetDocumentByName(project, "Program.cs");

            SyntaxNode syntaxRoot = await csProgram.GetSyntaxRootAsync();
            MethodNames rewriterMethods = new MethodNames();
            SyntaxNode newSyntaxRoot = rewriterMethods.Visit(syntaxRoot);

            SwitchCases rewriterSwitchCases = new SwitchCases();
            newSyntaxRoot = rewriterSwitchCases.Visit(newSyntaxRoot);

            solution = solution.WithDocumentSyntaxRoot(csProgram.Id, newSyntaxRoot);
            return solution;
        }

        protected async Task<Solution> RemoveHelpText(Solution solution, Project project)
        {
            project = VisualStudioManager.GetProjectByName(solution, project.Name);
            Document csProgram = VisualStudioManager.GetDocumentByName(project, "Program.cs");
            if (csProgram == null)
            {
                Logger.Error("SharpView Profile: Could not find Program.cs");
                return solution;
            }
            SyntaxNode syntaxRoot = await csProgram.GetSyntaxRootAsync();
            RemoveHelpText rewriterHelpUsage = new RemoveHelpText();
            SyntaxNode newSyntaxRoot = rewriterHelpUsage.Visit(syntaxRoot);

            solution = solution.WithDocumentSyntaxRoot(csProgram.Id, newSyntaxRoot);
            return solution;
        }
    }
}
