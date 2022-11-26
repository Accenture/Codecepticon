using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace Codecepticon.Modules.CSharp
{
    class SyntaxTreeHelper
    {
        public SyntaxNode FindChildOfType(SyntaxNode node, SyntaxKind syntaxKind)
        {
            return FindChildOfType(node, new[] { syntaxKind });
        }

        public SyntaxNode FindChildOfType(SyntaxNode node, SyntaxKind[] syntaxKinds)
        {
            SyntaxNode pChild = null;

            foreach (SyntaxNode pNode in node.ChildNodes())
            {
                if (syntaxKinds.Any(s => pNode.IsKind(s)))
                {
                    pChild = pNode;
                    break;
                }
            }

            return pChild;
        }

        public List<SyntaxNode> FindChildrenOfType(SyntaxNode node, SyntaxKind syntaxKind)
        {
            return FindChildrenOfType(node, new[] { syntaxKind });
        }

        public List<SyntaxNode> FindChildrenOfType(SyntaxNode node, SyntaxKind[] syntaxKinds)
        {
            List<SyntaxNode> pChildren = new List<SyntaxNode>();

            foreach (SyntaxNode pNode in node.ChildNodes())
            {
                if (syntaxKinds.Any(s => pNode.IsKind(s)))
                {
                    pChildren.Add(pNode);
                }
            }

            return pChildren;
        }

        public bool HasTokenOfType(SyntaxNode node, SyntaxKind token)
        {
            return HasTokenOfType(node, new[] { token });
        }

        public bool HasTokenOfType(SyntaxNode node, SyntaxKind[] tokens)
        {
            bool exists = false;

            foreach (SyntaxToken token in node.ChildTokens())
            {
                if (tokens.Any(t => token.IsKind(t)))
                {
                    exists = true;
                    break;
                }
            }

            return exists;
        }

        public bool HasChildOfType(SyntaxNode node, SyntaxKind kind)
        {
            return HasChildOfType(node, new[] { kind });
        }

        public bool HasChildOfType(SyntaxNode node, SyntaxKind[] kinds)
        {
            bool exists = false;
            foreach (SyntaxNode child in node.ChildNodes())
            {
                if (kinds.Any(kind => child.IsKind(kind)))
                {
                    exists = true;
                    break;
                }
            }

            return exists;
        }

        public bool IsOverride(SyntaxNode node)
        {
            return HasTokenOfType(node, SyntaxKind.OverrideKeyword);
        }

        public bool IsExternal(SyntaxNode node)
        {
            return HasTokenOfType(node, SyntaxKind.ExternKeyword);
        }

        private bool IsReservedNamespace(string name)
        {
            List<string> reservedNamespaces = new List<string>
            {
                "System",
                "Microsoft",
                "NtApiDotNet"
            };
            return reservedNamespaces.Contains(name);
        }

        public bool IsInterfaceImplementation(IPropertySymbol property)
        {
            var allInterfaces = property.ContainingType.AllInterfaces.SelectMany(@interface => @interface.GetMembers().OfType<IPropertySymbol>());
            bool result = false;
            foreach (IPropertySymbol item in allInterfaces)
            {
                if (property.ContainingType.FindImplementationForInterfaceMember(item) == null)
                {
                    continue;
                }

                if (property.ContainingType.FindImplementationForInterfaceMember(item).Equals(property))
                {
                    if (!IsReservedNamespace(GetParentNamespace(item)))
                    {
                        // This means that it IS an interface, but not a system one, therefore we can rename it.
                        continue;
                    }
                    // FYI: item.ContainingSymbol.Name = Interface Name.
                    result = true;
                    break;
                }
            }
            return result;
        }

        public bool IsInterfaceImplementation(IMethodSymbol method)
        {
            var allInterfaces = method.ContainingType.AllInterfaces.SelectMany(@interface => @interface.GetMembers().OfType<IMethodSymbol>());
            bool result = false;
            foreach (IMethodSymbol item in allInterfaces)
            {
                if (method.ContainingType.FindImplementationForInterfaceMember(item) == null)
                {
                    continue;
                }
                
                if (method.ContainingType.FindImplementationForInterfaceMember(item).Equals(method))
                {
                    if (!IsReservedNamespace(GetParentNamespace(item)))
                    {
                        // This means that it IS an interface, but not a system one, therefore we can rename it.
                        continue;
                    }
                    // FYI: item.ContainingSymbol.Name = Interface Name.
                    result = true;
                    break;
                }
            }
            return result;
        }

        public string GetParentNamespace(IPropertySymbol property)
        {
            string name = property.ContainingNamespace.Name;
            INamespaceSymbol pNamespace = property.ContainingNamespace;
            return FindParentNamespace(name, pNamespace);
        }

        public string GetParentNamespace(IMethodSymbol method)
        {
            string name = method.ContainingNamespace.Name;
            INamespaceSymbol pNamespace = method.ContainingNamespace;
            return FindParentNamespace(name, pNamespace);
        }

        private string FindParentNamespace(string name, INamespaceSymbol pNamespace)
        {
            do
            {
                if (pNamespace == null)
                {
                    break;
                }
                name = pNamespace.Name;
                pNamespace = pNamespace.ContainingNamespace;
            } while (pNamespace.Name != "");
            return name;
        }

        public async Task<Solution> RenameCode<T>(Solution solution, string projectName, string documentName, string existingName, string newName)
        {
            SyntaxNode node;

            Document document = VisualStudioManager.GetDocumentByName(solution, projectName, documentName);

            SemanticModel semanticModel = await document.GetSemanticModelAsync();
            SyntaxTree syntaxTree = await document.GetSyntaxTreeAsync();

            IEnumerable<T> nodes = syntaxTree.GetRoot().DescendantNodes().OfType<T>();

            if (typeof(T) == typeof(NamespaceDeclarationSyntax))
            {
                node = ((IEnumerable<NamespaceDeclarationSyntax>)nodes).FirstOrDefault(s => s.Name.ToString() == existingName);
            }
            else if (typeof(T) == typeof(ClassDeclarationSyntax))
            {
                node = ((IEnumerable<ClassDeclarationSyntax>)nodes).FirstOrDefault(s => s.Identifier.ToString() == existingName);
            }
            else if (typeof(T) == typeof(DelegateDeclarationSyntax))
            {
                node = ((IEnumerable<DelegateDeclarationSyntax>)nodes).FirstOrDefault(s => s.Identifier.ToString() == existingName);
            }
            else if (typeof(T) == typeof(MethodDeclarationSyntax))
            {
                node = ((IEnumerable<MethodDeclarationSyntax>)nodes).FirstOrDefault(s => s.Identifier.ToString() == existingName);
            }
            else if (typeof(T) == typeof(PropertyDeclarationSyntax))
            {
                node = ((IEnumerable<PropertyDeclarationSyntax>)nodes).FirstOrDefault(s => s.Identifier.ToString() == existingName);
            }
            else if (typeof(T) == typeof(EnumMemberDeclarationSyntax))
            {
                node = ((IEnumerable<EnumMemberDeclarationSyntax>)nodes).FirstOrDefault(s => s.Identifier.ToString() == existingName);
            }
            else if (typeof(T) == typeof(VariableDeclaratorSyntax))
            {
                node = ((IEnumerable<VariableDeclaratorSyntax>)nodes).FirstOrDefault(s => s.Identifier.ToString() == existingName);
            }
            else if (typeof(T) == typeof(ParameterSyntax))
            {
                node = ((IEnumerable<ParameterSyntax>)nodes).FirstOrDefault(s => s.Identifier.ToString() == existingName);
            }
            else
            {
                node = ((IEnumerable<BaseTypeDeclarationSyntax>)nodes).FirstOrDefault(s => s.Identifier.ToString() == existingName);
            }

            if (node == null)
            {
                return solution;
            }

            ISymbol symbol = semanticModel.GetDeclaredSymbol(node);
            return await Renamer.RenameSymbolAsync(solution, symbol, newName, solution.Workspace.Options);
        }

        public List<string> GetSwitchCaseConditions(SyntaxNode node)
        {
            List<string> conditions = new List<string>();
            foreach (SyntaxNode label in node.ChildNodes())
            {
                if (!label.IsKind(SyntaxKind.CaseSwitchLabel))
                {
                    continue;
                }
                conditions.Add(label.ChildNodes().First().GetText().ToString().Trim());
            }

            return conditions;
        }

        public string GetSwitchCaseSourceCode(SyntaxNode node)
        {
            string code = "";
            foreach (SyntaxNode switchNode in node.ChildNodes())
            {
                if (switchNode.IsKind(SyntaxKind.CaseSwitchLabel) || switchNode.IsKind(SyntaxKind.BreakStatement) || switchNode.IsKind(SyntaxKind.DefaultSwitchLabel))
                {
                    continue;
                } else if (switchNode.IsKind(SyntaxKind.Block))
                {
                    code += GetSwitchCaseSourceCode(switchNode);
                }
                else
                {
                    code += switchNode.GetText().ToString();
                }
            }
            return code;
        }

        public SyntaxNode AddUsingStatement(SyntaxNode root, string name)
        {
            CompilationUnitSyntax rootCompilation = (CompilationUnitSyntax)root;
            if (rootCompilation.Usings.Any(u => u.Name.ToString() == name))
            {
                // using statement already exists.
                return root;
            }

            UsingDirectiveSyntax usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(name));
            rootCompilation = rootCompilation.AddUsings(usingDirective).NormalizeWhitespace();
            return rootCompilation.SyntaxTree.GetRoot();
        }

        public bool IsAssemblyAttribute(SyntaxNode node)
        {
            SyntaxNode pNode = FindParentOfType(node, SyntaxKind.AttributeList);
            return (pNode != null);
        }

        public SyntaxNode FindParentOfType(SyntaxNode node, SyntaxKind syntaxKind)
        {
            return FindParentOfType(node, new[] { syntaxKind });
        }

        public SyntaxNode FindParentOfType(SyntaxNode node, SyntaxKind[] syntaxKinds)
        {
            SyntaxNode pNode = node.Parent;
            SyntaxNode pParent = null;

            do
            {
                if (pNode == null)
                {
                    break;
                }

                if (syntaxKinds.Any(s => pNode.IsKind(s)))
                {
                    pParent = pNode;
                    break;
                }

                pNode = pNode.Parent;
            } while (pParent == null && pNode != null);

            return pParent;
        }

        public bool IsConstStatement(SyntaxNode node)
        {
            SyntaxNode pField = FindParentOfType(node, new[] { SyntaxKind.FieldDeclaration, SyntaxKind.LocalDeclarationStatement });

            if (pField == null)
            {
                return false;
            }

            bool isConst = false;
            foreach (SyntaxToken t in pField.ChildTokens())
            {
                if (t.IsKind(SyntaxKind.ConstKeyword))
                {
                    isConst = true;
                    break;
                }
            }

            return isConst;
        }

        public bool CanReplaceString(SyntaxNode node)
        {
            bool canReplace = true;
            SyntaxNode pNode = node.Parent;
            do
            {
                if (pNode == null)
                {
                    break;
                }

                // The SyntaxKind below is required to be a constant, therefore we cannot replace it with dynamic code (like a b64 decode).
                switch (pNode.Kind())
                {
                    case SyntaxKind.Attribute:
                    case SyntaxKind.CaseSwitchLabel:
                    case SyntaxKind.Parameter:
                        canReplace = false;
                        break;
                    case SyntaxKind.VariableDeclaration:
                        if (IsConstStatement(pNode))
                        {
                            canReplace = false;
                        }
                        break;
                }
                pNode = pNode.Parent;
            } while (true);

            return canReplace;
        }

        public bool HasInterpolationFormat(SyntaxNode node)
        {
            SyntaxNode interpolation = FindChildOfType(node, SyntaxKind.Interpolation);
            if (interpolation == null)
            {
                return false;
            }

            SyntaxNode interpolationFormatClause = FindChildOfType(interpolation, SyntaxKind.InterpolationFormatClause);
            return (interpolationFormatClause != null);
        }

        public string GetAssemblyPropertyName(SyntaxNode node)
        {
            string name = "";

            SyntaxNode pNode = FindParentOfType(node, SyntaxKind.Attribute);

            foreach (SyntaxNode n in pNode.ChildNodes())
            {
                if (n.IsKind(SyntaxKind.IdentifierName))
                {
                    name = n.ChildTokens().First().Value.ToString();
                    break;
                }
            }

            return name;
        }

        public string GetPropertyDeclaration(SyntaxNode node)
        {
            SyntaxNode propertyDeclaration = FindParentOfType(node, SyntaxKind.PropertyDeclaration);
            if (propertyDeclaration == null)
            {
                return "";
            }

            string variable = "";
            foreach (SyntaxToken t in propertyDeclaration.ChildTokens())
            {
                if (t.IsKind(SyntaxKind.IdentifierToken))
                {
                    variable = t.ValueText.Trim();
                    break;
                }
            }

            return variable;
        }

        public SyntaxNode RewriteCommandLineArg(SyntaxNode node, string text, string prepend)
        {
            if (!DataCollector.Mapping.CommandLine.ContainsKey(text))
            {
                DataCollector.CommandLine data = new DataCollector.CommandLine
                {
                    Argument = text,
                    NewName = CommandLineData.Global.NameGenerator.Generate().ToLower()
                };

                if (prepend.Length > 0)
                {
                    data.NewName = prepend + data.NewName;
                }
                DataCollector.Mapping.CommandLine.Add(text, data);
            }

            string code = $"\"{DataCollector.Mapping.CommandLine[text].NewName}\"";
            return SyntaxFactory.ParseExpression(code);
        }

        public string GetClassBaseList(SyntaxNode node)
        {
            SyntaxNode parentClass = FindParentOfType(node, SyntaxKind.ClassDeclaration);
            if (parentClass == null)
            {
                return "";
            }

            SyntaxNode baseList = FindChildOfType(parentClass, SyntaxKind.BaseList);
            if (baseList == null)
            {
                return "";
            }

            SyntaxNode baseType = FindChildOfType(baseList, SyntaxKind.SimpleBaseType);
            return baseType == null ? "" : baseType.ToString().Trim();
        }

        public bool IsOptionChild(SyntaxNode node)
        {
            SyntaxNode attributeList = FindChildOfType(node, SyntaxKind.AttributeList);
            if (attributeList == null)
            {
                return false;
            }

            SyntaxNode attribute = FindChildOfType(attributeList, SyntaxKind.Attribute);
            if (attribute == null)
            {
                return false;
            }

            SyntaxNode identifierName = FindChildOfType(attribute, SyntaxKind.IdentifierName);
            if (identifierName == null)
            {
                return false;
            }

            return (identifierName.ToString() == "Option");
        }

        public bool IsOptionParent(SyntaxNode node)
        {
            SyntaxNode attribute = FindParentOfType(node, SyntaxKind.Attribute);
            if (attribute == null)
            {
                return false;
            }

            SyntaxNode identifierName = FindChildOfType(attribute, SyntaxKind.IdentifierName);
            if (identifierName == null)
            {
                return false;
            }

            if (identifierName.GetText().ToString() != "Option")
            {
                return false;
            }
            return true;
        }

        public DataCollector.CommandLine GetOptionData(SyntaxNode node)
        {
            DataCollector.CommandLine data = new DataCollector.CommandLine
            {
                Argument = "",
                NewName = "",
                HelpText = "",
                PropertyName = ""
            };

            SyntaxNode attributeList = FindChildOfType(node, SyntaxKind.AttributeList);
            if (attributeList == null)
            {
                return data;
            }

            SyntaxNode attribute = FindChildOfType(attributeList, SyntaxKind.Attribute);
            if (attribute == null)
            {
                return data;
            }

            SyntaxNode attributeArgumentList = FindChildOfType(attribute, SyntaxKind.AttributeArgumentList);
            if (attributeArgumentList == null)
            {
                return data;
            }

            GetOptionArgument(node, attributeArgumentList, ref data);

            Dictionary<string, string> optionAttributes = GetOptionAttributes(attributeArgumentList);
            if (optionAttributes.ContainsKey("HelpText"))
            {
                data.HelpText = optionAttributes["HelpText"];
            }

            return data;
        }

        public void GetOptionArgument(SyntaxNode node, SyntaxNode attributeArgumentList, ref DataCollector.CommandLine data)
        {
            string argument = "";
            string propertyName = "";

            // We need to figure out which function overload has been called, in order to determine the argument name.
            int count = 0;
            foreach (SyntaxNode arg in attributeArgumentList.ChildNodes())
            {
                ++count;
                if (arg.ChildNodes().FirstOrDefault().IsKind(SyntaxKind.NameEquals))
                {
                    if (count == 1) // First argument.
                    {
                        // Defining properties straight away, so the argument's name is the variable name itself.
                        break;
                    }
                    else
                    {
                        SyntaxNode previousArg = attributeArgumentList.ChildNodes().Skip(count - 2).First();
                        argument = previousArg.ChildNodes().First().ChildTokens().First().ValueText.Trim();
                        break;
                    }
                }
            }

            foreach (SyntaxToken t in node.ChildTokens())
            {
                if (t.IsKind(SyntaxKind.IdentifierToken))
                {
                    propertyName = t.ToString();
                    break;
                }
            }

            data.Argument = argument.Length > 0 ? argument : propertyName;
            data.PropertyName = propertyName;
        }

        public Dictionary<string, string> GetOptionAttributes(SyntaxNode node)
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>();

            foreach (SyntaxNode attrArgument in node.ChildNodes())
            {
                if (!attrArgument.IsKind(SyntaxKind.AttributeArgument))
                {
                    continue;
                }

                string propertyName = "";
                string propertyValue = "";

                var nameEquals = attrArgument.DescendantNodes().OfType<NameEqualsSyntax>().FirstOrDefault();
                if (nameEquals != null)
                {
                    propertyName = nameEquals.ChildNodes().First().GetText().ToString().Trim();
                    SyntaxNode valueNode = attrArgument.ChildNodes().Skip(1).FirstOrDefault();
                    if (valueNode != null)
                    {
                        switch (valueNode.Kind())
                        {
                            case SyntaxKind.StringLiteralExpression:
                            case SyntaxKind.FalseLiteralExpression:
                            case SyntaxKind.TrueLiteralExpression:
                            case SyntaxKind.NullLiteralExpression:
                            case SyntaxKind.NumericLiteralExpression:
                                propertyValue = valueNode.ChildTokens().FirstOrDefault().ValueText.Trim();
                                break;
                            default:
                                propertyValue = valueNode.GetText().ToString().Trim();
                                break;
                        }
                    }
                }

                if (propertyName != "" && propertyValue != "")
                {
                    attributes.Add(propertyName, propertyValue);
                }
            }

            return attributes;
        }

        public string GetArgumentName(SyntaxNode node)
        {
            SyntaxNode argument = FindParentOfType(node, SyntaxKind.AttributeArgument);
            if (argument == null)
            {
                return "";
            }

            SyntaxNode nameEquals = FindChildOfType(argument, SyntaxKind.NameEquals);
            return nameEquals == null ? "" : nameEquals.ChildNodes().First().GetText().ToString().Trim();
        }

        public (string, string) GetAssignmentData(SyntaxNode node)
        {
            SyntaxNode identifierName = FindChildOfType(node, SyntaxKind.IdentifierName);
            SyntaxNode stringLiteral = FindChildOfType(node, SyntaxKind.StringLiteralExpression);
            if (identifierName == null || stringLiteral == null)
            {
                return ("", "");
            }

            string variable = identifierName.GetFirstToken().ValueText.Trim();
            string value = stringLiteral.GetFirstToken().ValueText.Trim();

            return (variable, value);
        }

        public string GetMethodMappedName(SyntaxNode node)
        {
            string methodName = "";
            SyntaxNode expressionStatement = FindChildOfType(node, SyntaxKind.ExpressionStatement);
            if (expressionStatement == null)
            {
                return "";
            }

            SyntaxNode simpleAssignmentExpression = FindChildOfType(expressionStatement, SyntaxKind.SimpleAssignmentExpression);
            if (simpleAssignmentExpression == null)
            {
                return "";
            }

            (string variable, string value) = GetAssignmentData(simpleAssignmentExpression);
            if (variable == "methodName")
            {
                methodName = value;
            }

            return methodName;
        }

        public string GetCallingFunctionNameFromArgument(SyntaxNode node)
        {
            if (!node.Parent.IsKind(SyntaxKind.Argument))
            {
                return "";
            }

            SyntaxNode invocationNode = FindParentOfType(node, SyntaxKind.InvocationExpression);
            if (invocationNode == null)
            {
                return "";
            }

            SyntaxNode identifier = FindChildOfType(invocationNode, SyntaxKind.IdentifierName);
            if (identifier == null)
            {
                return "";
            }

            return identifier.GetText().ToString();
        }
    }
}
