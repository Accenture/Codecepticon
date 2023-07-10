using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.Utils;

namespace Codecepticon.Modules.CSharp
{
    class DataRenamer : SyntaxTreeHelper
    {
        public async Task<Solution> RenameNamespaces(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var namespaces = syntaxTree.GetRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>();

            foreach (var n in namespaces)
            {
                string name = n.Name.ToString();
                if (!DataCollector.Mapping.Namespaces.ContainsKey(name))
                {
                    Logger.Debug($"Namespace does not exist in mapping: {name}");
                    continue;
                }

                solution = await RenameCode<NamespaceDeclarationSyntax>(solution, projectName, documentName, name, DataCollector.Mapping.Namespaces[name]);
            }

            return solution;
        }

        public async Task<Solution> RenameClasses(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var classes = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var c in classes)
            {
                string name = c.Identifier.ToString();
                if (!DataCollector.Mapping.Classes.ContainsKey(name))
                {
                    Logger.Debug($"Class does not exist in mapping: {name}");
                    continue;
                }

                solution = await RenameCode<ClassDeclarationSyntax>(solution, projectName, documentName, name, DataCollector.Mapping.Classes[name]);
            }

            return solution;
        }

        public async Task<Solution> RenameEnums(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var enums = syntaxTree.GetRoot().DescendantNodes().OfType<EnumDeclarationSyntax>();

            foreach (var e in enums)
            {
                string name = e.Identifier.ToString();
                if (!DataCollector.Mapping.Enums.ContainsKey(name))
                {
                    Logger.Debug($"Enum Declaration does not exist in mapping: {name}");
                    continue;
                }
                
                solution = await RenameCode<EnumDeclarationSyntax>(solution, projectName, documentName, name, DataCollector.Mapping.Enums[name]);
            }

            var enums2 = syntaxTree.GetRoot().DescendantNodes().OfType<EnumMemberDeclarationSyntax>();

            foreach (var e in enums2)
            {
                string name = e.Identifier.ToString();
                if (!DataCollector.Mapping.Enums.ContainsKey(name))
                {
                    Logger.Debug($"Enum Member does not exist in mapping: {name}");
                    continue;
                }

                solution = await RenameCode<EnumMemberDeclarationSyntax>(solution, projectName, documentName, name, DataCollector.Mapping.Enums[name]);
            }

            return solution;
        }

        public async Task<Solution> RenameFunctions(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);
            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();

            var methods = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var m in methods)
            {
                string name = m.Identifier.ToString();
                if (!DataCollector.Mapping.Functions.ContainsKey(name))
                {
                    Logger.Debug($"Function does not exist in mapping: {name}");

                    continue;
                }

                solution = await RenameCode<MethodDeclarationSyntax>(solution, projectName, documentName, name, DataCollector.Mapping.Functions[name]);
            }

            // Rename delegates.
            var delegateMethods = syntaxTree.GetRoot().DescendantNodes().OfType<DelegateDeclarationSyntax>();
            foreach(var m in delegateMethods)
            {
                string name = m.Identifier.ToString();
                if (!DataCollector.Mapping.Functions.ContainsKey(name))
                {
                    Logger.Debug($"Delegate Function does not exist in mapping: {name}");

                    continue;
                }

                solution = await RenameCode<DelegateDeclarationSyntax>(solution, projectName, documentName, name, DataCollector.Mapping.Functions[name]);
            }

            return solution;
        }

        public async Task<Solution> RenameProperties(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);
            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();

            var properties = syntaxTree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var p in properties)
            {
                string name = p.Identifier.ToString();
                if (!DataCollector.Mapping.Properties.ContainsKey(name))
                {
                    Logger.Debug($"Property does not exist in mapping: {name}");
                    continue;
                }

                solution = await RenameCode<PropertyDeclarationSyntax>(solution, projectName, documentName, name, DataCollector.Mapping.Properties[name]);
            }

            return solution;
        }

        public async Task<Solution> RenameParameters(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var parameters = syntaxTree.GetRoot().DescendantNodes().OfType<ParameterSyntax>();
            foreach (var p in parameters)
            {
                string name = p.Identifier.ToString();
                if (!DataCollector.Mapping.Parameters.ContainsKey(name))
                {
                    Logger.Debug($"Parameter does not exist in mapping: {name}");
                    continue;
                }

                solution = await RenameCode<ParameterSyntax>(solution, projectName, documentName, name, DataCollector.Mapping.Parameters[name]);
            }

            return solution;
        }

        public async Task<Solution> RenameVariables(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var variables = syntaxTree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>();
            foreach (var v in variables)
            {
                string name = v.Identifier.ToString();
                if (!DataCollector.Mapping.Variables.ContainsKey(name))
                {
                    Logger.Debug($"Variable does not exist in mapping: {name}");
                    continue;
                }

                solution = await RenameCode<VariableDeclaratorSyntax>(solution, projectName, documentName, name, DataCollector.Mapping.Variables[name]);
            }

            return solution;
        }

        public async Task<Solution> RenameStructs(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var structs = syntaxTree.GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>();
            foreach (var s in structs)
            {
                string name = s.Identifier.ToString();
                if (!DataCollector.Mapping.Structs.ContainsKey(name))
                {
                    Logger.Debug($"Struct does not exist in mapping: {name}");
                    continue;
                }

                solution = await RenameCode<StructDeclarationSyntax>(solution, projectName, documentName, name, DataCollector.Mapping.Structs[name]);
            }

            return solution;
        }
    }
}
