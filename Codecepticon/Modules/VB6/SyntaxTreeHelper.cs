using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Codecepticon.Utils;

namespace Codecepticon.Modules.VB6
{
    class SyntaxTreeHelper
    {
        public string GetStatementIdentifier(ParserRuleContext context)
        {
            string name = "";
            foreach (var child in context.children)
            {
                if ((int)GetObjectPropertyValue(child, "RuleIndex", 0) == VisualBasic6Parser.RULE_ambiguousIdentifier)
                {
                    name = child.GetText();
                    break;
                }
            }
            return name;
        }

        public string GetCustomVariableType(VisualBasic6Parser.TypeContext context)
        {
            return Vb6Constants.DataTypes.Contains(context.GetText().ToLower()) ? "" : context.GetText();
        }

        public bool IsReserved(string name)
        {
            name = name.ToLower();
            if (name.StartsWith("document_"))
            {
                Logger.Debug($"Skipping rename of {name}");
                // Functions like Document_Open, Document_XMLAfterInsert, etc.
                return true;
            } else if (name.StartsWith("worksheet_"))
            {
                Logger.Debug($"Skipping rename of {name}");
                // Functions like Workbook_Activate, Workbook_BeforeSave, etc.
                return true;
            }
            return OfficeReservedWords.FileMacroFunctions.Contains(name.ToLower());
        }

        public bool ObjectHasProperty(IParseTree context, string propertyName)
        {
            return context.GetType().GetProperty(propertyName) != null;
        }

        public object GetObjectPropertyValue(IParseTree context, string propertyName, object defaultValue)
        {
            return ObjectHasProperty(context, propertyName) ? context.GetType().GetProperty(propertyName).GetValue(context, null) : defaultValue;
        }

        public bool DeclarationHasAlias(ParserRuleContext context)
        {
            bool hasAlias = false;
            foreach (var child in context.children)
            {
                if (child.GetText().ToLower() == "alias")
                {
                    hasAlias = true;
                    break;
                }
            }

            return hasAlias;
        }

        public bool ChildObjectHasProperty(IParseTree context, string propertyName)
        {
            return context.Payload.GetType().GetProperty(propertyName) != null;
        }

        public object GetChildObjectPropertyValue(IParseTree context, string propertyName, object defaultValue)
        {
            return ChildObjectHasProperty(context, propertyName) ? context.Payload.GetType().GetProperty(propertyName).GetValue(context.Payload, null) : defaultValue;
        }

        public int GetChildType(IParseTree context)
        {
            var type = GetChildObjectPropertyValue(context, "Type", 0);
            if (type == null)
            {
                return 0;
            }
            return Int32.Parse(type.ToString());
        }

        public bool IsDeclarationLibName(IList<IParseTree> children, int index)
        {
            bool isLibName = false;
            for (int i = 0; i < index; i++)
            {
                if (GetChildType(children[i]) == VisualBasic6Parser.LIB)
                {
                    isLibName = true;
                    break;
                }
            }
            return isLibName;
        }

        public string DeclarationAddAlias(ParserRuleContext context)
        {
            string newDeclaration = "";
            for (int i = 0; i < context.ChildCount; i++)
            {
                var child = context.children[i];
                if (GetChildType(child) == VisualBasic6Parser.STRINGLITERAL)
                {
                    if (IsDeclarationLibName(context.children, i))
                    {
                        newDeclaration += child.GetText() + " Alias \""+ GetStatementIdentifier(context) +"\"";
                        continue;
                    }
                }
                newDeclaration += child.GetText();
            }
            return newDeclaration;
        }

        public bool IsStringConstant(ParserRuleContext context)
        {
            // Go up to 5 parents to see if we hit a const statement.
            RuleContext original = context;
            bool result = false;
            for (int i = 0; i < 5; i++)
            {
                original = original.Parent != null ? original.Parent : null;
                if (original != null && (int)GetObjectPropertyValue(original, "RuleIndex", 0) == VisualBasic6Parser.RULE_constStmt)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
    }
}
