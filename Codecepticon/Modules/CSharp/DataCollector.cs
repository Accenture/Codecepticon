using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Codecepticon.Utils;

namespace Codecepticon.Modules.CSharp
{
    class DataCollector
    {
        public static List<string> AllNamespaces = new List<string>();
        public static List<string> AllClasses = new List<string>();
        public static List<string> AllEnums = new List<string>();
        public static List<string> AllFunctions = new List<string>();
        public static List<string> AllInterfaces = new List<string>();
        public static List<string> AllOverridesOrExternalFunctions = new List<string>();
        public static List<string> AllOverridesOrExternalProperties = new List<string>();
        public static List<string> AllProperties = new List<string>();
        public static List<string> AllVariables = new List<string>();
        public static List<string> AllParameters = new List<string>();
        public static List<string> AllStructs = new List<string>();

        public struct CommandLine
        {
            public string Argument;
            public string HelpText;
            public string NewName;
            public string PropertyName;
        }

        public struct MappingStruct
        {
            public Dictionary<string, string> Namespaces;
            public Dictionary<string, string> Functions;
            public Dictionary<string, string> Classes;
            public Dictionary<string, string> Enums;
            public Dictionary<string, string> Properties;
            public Dictionary<string, string> Variables;
            public Dictionary<string, string> Parameters;
            public Dictionary<string, string> Structs;
            public Dictionary<string, CommandLine> CommandLine;
        }

        public static MappingStruct Mapping = new MappingStruct
        {
            Namespaces = new Dictionary<string, string>(),
            Functions = new Dictionary<string, string>(),
            Classes = new Dictionary<string, string>(),
            Enums = new Dictionary<string, string>(),
            Properties = new Dictionary<string, string>(),
            Variables = new Dictionary<string, string>(),
            Parameters = new Dictionary<string, string>(),
            Structs = new Dictionary<string, string>(),
        };

        public static bool IsMappingUnique(string name)
        {
            string item = Mapping.Namespaces.FirstOrDefault(s => s.Value == name).Key;
            if (item != null)
            {
                return false;
            }

            item = Mapping.Functions.FirstOrDefault(s => s.Value == name).Key;
            if (item != null)
            {
                return false;
            }

            item = Mapping.Classes.FirstOrDefault(s => s.Value == name).Key;
            if (item != null)
            {
                return false;
            }

            item = Mapping.Enums.FirstOrDefault(s => s.Value == name).Key;
            if (item != null)
            {
                return false;
            }

            item = Mapping.Properties.FirstOrDefault(s => s.Value == name).Key;
            if (item != null)
            {
                return false;
            }

            item = Mapping.Variables.FirstOrDefault(s => s.Value == name).Key;
            if (item != null)
            {
                return false;
            }

            item = Mapping.Structs.FirstOrDefault(s => s.Value == name).Key;
            if (item != null)
            {
                return false;
            }

            item = Mapping.Parameters.FirstOrDefault(s => s.Value == name).Key;
            return item == null;
        }

        public static async Task CollectNamespaces(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var namespaces = syntaxTree.GetRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>();

            foreach (var n in namespaces)
            {
                string name = n.Name.ToString();

                if (name == "System")
                {
                    continue;
                } else if (name == "NtApiDotNet")
                {
                    continue;
                }
                
                if (AllNamespaces.Contains(name))
                {
                    continue;
                }

                AllNamespaces.Add(name);
            }
        }

        public static async Task CollectClasses(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var classes = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var c in classes)
            {
                string name = c.Identifier.ToString();
                if (AllClasses.Contains(name))
                {
                    continue;
                }
                AllClasses.Add(name);
            }
        }

        public static async Task CollectEnums(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var enums = syntaxTree.GetRoot().DescendantNodes().OfType<EnumDeclarationSyntax>();

            foreach (var e in enums)
            {
                string name = e.Identifier.ToString();
                if (AllEnums.Contains(name))
                {
                    continue;
                }
                AllEnums.Add(name);
            }

            var enumMembers = syntaxTree.GetRoot().DescendantNodes().OfType<EnumMemberDeclarationSyntax>();
            foreach (var e in enumMembers)
            {
                string name = e.Identifier.ToString();
                if (AllEnums.Contains(name))
                {
                    continue;
                }
                AllEnums.Add(name);
            }
        }

        public static async Task CollectParameters(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var parameters = syntaxTree.GetRoot().DescendantNodes().OfType<ParameterSyntax>();

            foreach (var p in parameters)
            {
                string name = p.Identifier.ToString();
                if (name == "_")
                {
                    continue;
                }

                if (AllParameters.Contains(name))
                {
                    continue;
                }
                AllParameters.Add(name);
            }
        }

        public static async Task CollectStructs(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);
            
            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var structs = syntaxTree.GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>();

            foreach (var s in structs)
            {
                string name = s.Identifier.ToString();
                if (AllStructs.Contains(name))
                {
                    continue;
                }
                AllStructs.Add(name);
            }
        }

        public static async Task CollectProperties(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            SemanticModel semanticModel = await document.GetSemanticModelAsync();
            var properties = syntaxTree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>();

            SyntaxTreeHelper helper = new SyntaxTreeHelper();

            foreach (var p in properties)
            {
                string name = p.Identifier.ToString();
                IPropertySymbol methodSymbol = semanticModel.GetDeclaredSymbol(p);

                if (helper.IsInterfaceImplementation(methodSymbol))
                {
                    AllInterfaces.Add(name);
                    continue;
                }

                if (helper.IsOverride(p))
                {
                    AllOverridesOrExternalProperties.Add(name);
                    continue;
                }

                if (AllProperties.Contains(name))
                {
                    continue;
                }
                
                AllProperties.Add(name);
            }
        }

        public static async Task CollectVariables(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            var variables = syntaxTree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>();

            foreach (var v in variables)
            {
                string name = v.Identifier.ToString();
                if (AllVariables.Contains(name))
                {
                    continue;
                }
                AllVariables.Add(name);
            }
        }

        public static async Task CollectFunctions(Solution solution, string projectName, string documentName)
        {
            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();
            SemanticModel semanticModel = await document.GetSemanticModelAsync();
            var methods = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();

            SyntaxTreeHelper helper = new SyntaxTreeHelper();

            foreach (var m in methods)
            {
                string name = m.Identifier.ToString();
                IMethodSymbol methodSymbol = semanticModel.GetDeclaredSymbol(m);

                if (helper.IsOverride(m) || helper.IsExternal(m))
                {
                    AllOverridesOrExternalFunctions.Add(name);
                    continue;
                } 
                
                if (helper.IsInterfaceImplementation(methodSymbol))
                {
                    AllInterfaces.Add(name);
                    continue;
                } 
                
                if (AllFunctions.Contains(name))
                {
                    continue;
                }
                AllFunctions.Add(name);
            }

            // Also collect Delegate function declarations.
            var delegateMethods = syntaxTree.GetRoot().DescendantNodes().OfType<DelegateDeclarationSyntax>();
            foreach (var m in delegateMethods)
            {
                string name = m.Identifier.ToString();
                if (AllFunctions.Contains(name))
                {
                    continue;
                }
                AllFunctions.Add(name);
            }
        }

        public static async Task FilterCollectedData()
        {
            // Remove all override methods from the collected functions, because we can't rename those.
            foreach (var name in AllOverridesOrExternalFunctions)
            {
                Logger.Debug($"Removing OverrideOrExternalFunction: {name}");
                AllFunctions.Remove(name);
            }
            
            // Same with interfaces.
            foreach (var name in AllInterfaces)
            {
                Logger.Debug($"Removing Interface: {name}");
                AllFunctions.Remove(name);
            }

            // And last but not least, remove "Main".
            AllFunctions.Remove("Main");

            // Remove all override properties from the collected properties.
            foreach (var name in AllOverridesOrExternalProperties)
            {
                Logger.Debug($"Removing OverrideOrExternalProperty: {name}");
                AllProperties.Remove(name);
            }
        }

        public static string ConcatCommandLineData(Dictionary<string, CommandLine> commandLineData)
        {
            string data = "";
            foreach (KeyValuePair<string, CommandLine> item in commandLineData)
            {
                if (data.Length > 0)
                {
                    data += "|";
                }

                data += $"{item.Value.Argument}:{item.Value.NewName}";
            }
            return data;
        }
    }
}
