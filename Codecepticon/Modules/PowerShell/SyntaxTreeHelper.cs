using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.PowerShell
{
    class SyntaxTreeHelper
    {
        // https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_automatic_variables?view=powershell-7.2
        protected List<string> ReservedNames = new List<string>
        {
            "$false",
            "$true",
            "$null",
            "$_",
            "$?",
            "$args",
            "$consolefilename",
            "$error",
            "$errorview",
            "$event",
            "$eventargs",
            "$eventsubscriber",
            "$executioncontext",
            "$foreach",
            "$home",
            "$host",
            "$input",
            "$iscoreclr",
            "$islinux",
            "$ismacos",
            "$iswindows",
            "$lastexitcode",
            "$matches",
            "$myinvocation",
            "$nestedpromptlevel",
            "$pid",
            "$profile",
            "$psboundparameters",
            "$pscmdlet",
            "$pscommandpath",
            "$psculture",
            "$psdebugcontext",
            "$pshome",
            "$psitem",
            "$psnativecommandargumentpassing",
            "$psscriptroot",
            "$pssenderinfo",
            "$psstyle",
            "$psuiculture",
            "$psversiontable",
            "$pwd",
            "$sender",
            "$shellid",
            "$stacktrace",
            "$switch",
            "$this"
        };

        // https://docs.microsoft.com/en-us/powershell/scripting/developer/cmdlet/parameter-attribute-declaration?view=powershell-7.2
        // https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_functions_cmdletbindingattribute?view=powershell-7.2
        protected List<string> ReservedAttributeDeclarations = new List<string>
        {
            "position",
            "mandatory",
            "parametersetname",
            "valuefrompipeline",
            "valuefrompipelinebypropertyname",
            "valuefromremainingarguments",
            "helpmessage",
            "helpmessagebasename",
            "helpmessageresourceid",

            "confirmimpact",
            "defaultparametersetname",
            "helpuri",
            "supportspaging",
            "supportsshouldprocess",
            "positionalbinding"
        };

        protected List<string> SpecialCharacters = new List<string>
            {
                "`0",   // Null
                "`a",   // Alert
                "`b",   // Backspace
                "`e",   // Escape
                "`f",   // Form feed
                "`n",   // New line
                "`r",   // Carriage return
                "`t",   // Horizontal tab
                //"`u",   // Unicode escape sequence
                "`v",   // Vertical tab
            };

        public bool IsComment(Token token)
        {
            return token.Kind == TokenKind.Comment;
        }

        public bool IsSplattedVariable(Token token)
        {
            return token.Kind == TokenKind.SplattedVariable;
        }

        public bool IsFunctionCall(Token token)
        {
            return (token.TokenFlags == TokenFlags.CommandName) && (token.Kind == TokenKind.Identifier || token.Kind == TokenKind.Generic);
        }

        public bool IsGetCommandFunction(Token token)
        {
            return (token != null) && (token.TokenFlags == TokenFlags.CommandName && token.Kind == TokenKind.Generic && token.Text.ToLower() == "get-command");
        }

        public Token FindParentFunctionCall(Token[] psTokens, int currPos)
        {
            Token function = null;

            for (int i = currPos; i >= 0; i--)
            {
                if (IsFunctionCall(psTokens[i]))
                {
                    function = psTokens[i];
                    break;
                }
            }

            return function;
        }

        public bool IsCallingFunctionBuiltIn(Token[] psTokens, int currPos)
        {
            Token function = FindParentFunctionCall(psTokens, currPos);
            if (function == null)
            {
                return false;
            }

            return !DataCollector.AllFunctions.Contains(function.Text.ToLower());
        }

        public bool IsFunction(Token token)
        {
            if (token == null)
            {
                return false;
            }

            return token.Kind == TokenKind.Function;
        }

        public bool IsString(Token token)
        {
            return token.Kind == TokenKind.StringExpandable || token.Kind == TokenKind.StringLiteral;
        }

        public bool IsAssignment(Token[] psTokens, int currPos)
        {
            if (currPos + 1 < psTokens.Length)
            {
                if (psTokens[currPos + 1].TokenFlags == TokenFlags.AssignmentOperator)
                {
                    return true;
                }
            }

            if (currPos + 2 < psTokens.Length)
            {
                if (psTokens[currPos + 2].TokenFlags == TokenFlags.AssignmentOperator)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPSBoundReference(Token[] psTokens, int currPos)
        {
            if (currPos - 2 < 0)
            {
                return false;
            }

            return psTokens[currPos - 2].Text.ToLower() == "$PSBoundParameters".ToLower();
        }

        public bool IsVariableProperty(Token[] psTokens, int currPos)
        {
            return false;
        }

        public bool IsMemberName(Token token)
        {
            return token.TokenFlags == TokenFlags.MemberName;
        }

        public bool IsAssignment(Token token)
        {
            if (token == null)
            {
                return false;
            }

            return token.TokenFlags == TokenFlags.AssignmentOperator;
        }

        public bool CanReplaceString(Token[] psTokens, int currPos)
        {
            if (psTokens[currPos].Text == "''" || psTokens[currPos].Text == "\"\"")
            {
                return false;
            }
            else if (SpecialCharacters.Contains(psTokens[currPos].Text.Trim('\"')))
            {
                return false;
            }

            if (psTokens[currPos - 1].TokenFlags == TokenFlags.AssignmentOperator)
            {
                return CanReplaceMember(psTokens[currPos - 2]);
            }

            for (int i = currPos; i >= 0; i--)
            {
                if (psTokens[i].Kind == TokenKind.LParen)
                {
                    if (psTokens[i - 1].TokenFlags == (TokenFlags.TypeName | TokenFlags.AttributeName))
                    {
                        return false;
                    }

                    break;
                }
            }
            
            return true;
        }

        public bool CanReplaceMember(Token[] psTokens, int currPos)
        {
            if (!ReservedAttributeDeclarations.Contains(psTokens[currPos].Text.ToLower())) {
                // If it's not on the list, return true.
                return true;
            }
            // If it is, it means a name like "Position" is also used as a variable.
            for (int i = (currPos - 1); i >= (currPos - 5); i--)
            {
                if (psTokens[i].Kind == TokenKind.Identifier && psTokens[i].Text == "Parameter")
                {
                    return false;
                }
            }
            return true;
        }

        public bool CanReplaceMember(Token token)
        {
            return !ReservedAttributeDeclarations.Contains(token.Text.ToLower());
        }

        public bool IsSpecialOperator(Token token)
        {
            return token.Kind == TokenKind.Dot;
        }

        public string ExtractSpecialCharactersInStrings(string text)
        {
            // https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Special_Characters?view=powershell-7.2
            if (text.Length == 0 || text.IndexOf("`") < 0)
            {
                return text;
            }
            else if (text.Substring(0, 1) != "\"")
            {
                // Backticks only apply within double quotes.
                return text;
            }

            /*
             * "Hello`r`nWorld"
             * 
             * will become
             * 
             * "Hello" + "`r" + "`n" + "World!"
             */
            foreach (string special in SpecialCharacters)
            {
                text = text.Replace(special, $"\" + \"{special}\" + \"");
            }

            return text;
        }

        public string ExpandVariablesInStrings(string text)
        {
            if (text.IndexOf("$") < 0)
            {
                return text;
            }

            int c = 0;
            string item;
            List<string> vars = new List<string>();

            // First, extract any $(.*).
            do
            {
                item = ExtractStringExpansion(text);
                if (item == "")
                {
                    break;
                }

                text = text.Replace(item, "{" + c++ + "}");
                //item = item.Substring(0, item.Length - 1).Substring(2);
                vars.Add(item);
            } while (true);

            // Now extract direct variable references.
            do
            {
                item = GetVariableInString(text);
                if (item == "")
                {
                    break;
                }

                text = text.Replace(item, "{" + c++ + "}");
                vars.Add(item.Replace("{", "").Replace("}", ""));
            } while (true);

            // Extract $_ if it exists.
            if (text.IndexOf("$_") > -1)
            {
                item = "$_";
                text = text.Replace(item, "{" + c++ + "}");
                vars.Add(item);
            }

            if (vars.Any())
            {
                text = "(" + text + " -f " + String.Join(", ", vars) + ")";
            }

            return text;
        }

        protected string ExtractStringExpansion(string text)
        {
            if (text.IndexOf("$(") < 0)
            {
                return "";
            }

            int depth = 1;
            int p = text.IndexOf("$(");
            string output = "$(";
            for (int i = (p + 2); i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '(':
                        depth++;
                        break;
                    case ')':
                        depth--;
                        break;
                }

                output += text[i].ToString();
                if (depth == 0)
                {
                    break;
                }
            }

            return output;
        }

        public string GetVariableInString(string text)
        {
            string name = "";
            foreach (KeyValuePair<string, string> item in DataCollector.Mapping.Variables)
            {
                // First search if the variable is in a ${variable} format.
                if (text.ToLower().IndexOf($"{{{item.Key}}}") >= 0)
                {
                    name = $"${{{item.Key}}}";
                    break;
                }

                // Now search for the variable in its $variable format
                int p = text.ToLower().IndexOf("$" + item.Key);
                if (p == -1)
                {
                    continue;
                }

                // Check if the character after the end of the found string, is an invalid variable-name char.
                string value = text.Substring(p + item.Key.Length + 1); // +1 is for the $ sign.
                if (value.Length == 0)
                {
                    name = text.Substring(p, item.Key.Length + 1);
                    break;
                }

                value = value.Substring(0, 1);
                if (!(value == "_" || value.All(char.IsLetterOrDigit)))
                {
                    name = text.Substring(p, item.Key.Length + 1);
                    break;
                }
            }

            return name;
        }

        public bool IsVariable(Token token)
        {
            if (token.Kind != TokenKind.Variable)
            {
                return false;
            }
            
            return !ReservedNames.Contains(token.Text.ToLower());
        }

        public bool IsParameter(Token token)
        {
            return token.Kind == TokenKind.Parameter;
        }

        public (string, int) ReplaceItem(Token token, string replaceWith, string script, int padding)
        {
            string scriptLeft = script.Substring(0, token.Extent.StartOffset + padding);
            string scriptRight = script.Substring(token.Extent.EndOffset + padding);
            script = scriptLeft + replaceWith + scriptRight;

            int currLength = token.Extent.EndOffset - token.Extent.StartOffset;
            int lenDiff = replaceWith.Length - currLength;
            padding += lenDiff;

            return (script, padding);
        }

        public string ReplaceVariableInString(string originalText, string replaceWhat, string replaceWith)
        {
            // I'm doing it this way because there's no easy way to do a case-insensitive replace.
            int p = originalText.ToLower().IndexOf(replaceWhat.ToLower());
            if (p < 0)
            {
                return "";
            }
            originalText = originalText.Substring(0, p) + replaceWith + originalText.Substring(p + replaceWhat.Length);
            return originalText;
        }
    }
}
