using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Utils;
using Exception = System.Exception;

namespace Codecepticon.Modules.PowerShell
{
    class DataRenamer
    {
        public SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        protected NameGenerator NameGenerator = new NameGenerator(NameGenerator.RandomNameGeneratorMethods.RandomCombinations, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", 32);

        public async Task<string> ExpandStrings(string script, Token[] psTokens)
        {
            int padding = 0;
            int steps = psTokens.Length / 10;
            if (steps == 0) { steps = 1; }

            for (int i = 0; i < psTokens.Length; i++)
            {
                if (i % steps == 0)
                {
                    Logger.Info(".", false, false);
                }

                Token currToken = psTokens[i];

                if (Helper.IsString(currToken))
                {
                    string replaceWith = Helper.ExpandVariablesInStrings(currToken.Text);
                    if (replaceWith != currToken.Text)
                    {
                        (script, padding) = Helper.ReplaceItem(currToken, replaceWith, script, padding);
                    }
                }
            }

            return script;
        }

        public async Task<string> ExpandSpecialCharacters(string script, Token[] psTokens)
        {
            // Extract special characters like `r and `n.
            int padding = 0;
            int steps = psTokens.Length / 10;
            if (steps == 0) { steps = 1; }

            for (int i = 0; i < psTokens.Length; i++)
            {
                if (i % steps == 0)
                {
                    Logger.Info(".", false, false);
                }

                Token currToken = psTokens[i];

                if (Helper.IsString(currToken))
                {
                    string replaceWith = Helper.ExtractSpecialCharactersInStrings(currToken.Text);
                    if (replaceWith != currToken.Text)
                    {
                        (script, padding) = Helper.ReplaceItem(currToken, replaceWith, script, padding);
                    }
                }
            }

            return script;
        }

        public async Task<string> Rename(string scriptContents, Token[] psTokens)
        {
            int padding = 0;
            int steps = psTokens.Length / 10;
            if (steps == 0) { steps = 1; }
            string replaceWith = "";

            for (int i = 0; i < psTokens.Length; i++)
            {
                if (i % steps == 0)
                {
                    Logger.Info(".", false, false);
                }

                Token prevToken = (i == 0) ? null : psTokens[i - 1];
                Token currToken = psTokens[i];
                Token nextToken = (i + 1 < psTokens.Length) ? psTokens[i + 1] : null;

                if (Helper.IsComment(currToken))
                {
                    // Remove comments by default.
                    (scriptContents, padding) = Helper.ReplaceItem(currToken, "", scriptContents, padding);
                }
                else if (Helper.IsFunction(prevToken) || Helper.IsFunctionCall(currToken) || Helper.IsGetCommandFunction(prevToken))
                {
                    if (!CommandLineData.PowerShell.Rename.Functions)
                    {
                        continue;
                    }

                    string functionName = currToken.Text;
                    if (DataCollector.Mapping.Functions.ContainsKey(functionName.ToLower()))
                    {
                        (scriptContents, padding) = Helper.ReplaceItem(currToken, DataCollector.Mapping.Functions[functionName.ToLower()], scriptContents, padding);
                    }
                }
                else if (Helper.IsVariable(currToken))
                {
                    if (!CommandLineData.PowerShell.Rename.Variables)
                    {
                        continue;
                    }

                    string variableName = currToken.Text.Substring(1);
                    if (DataCollector.Mapping.Variables.ContainsKey(variableName.ToLower()))
                    {
                        replaceWith = "$" + DataCollector.Mapping.Variables[variableName.ToLower()];
                        (scriptContents, padding) = Helper.ReplaceItem(currToken, replaceWith, scriptContents, padding);
                    }
                }
                else if (Helper.IsMemberName(currToken) && Helper.IsAssignment(nextToken))
                {
                    if (!CommandLineData.PowerShell.Rename.Variables || !Helper.CanReplaceMember(psTokens, i) || Helper.IsSpecialOperator(prevToken))
                    {
                        continue;
                    }

                    string memberName = currToken.Text;
                    if (DataCollector.Mapping.Variables.ContainsKey(memberName.ToLower()))
                    {
                        replaceWith = DataCollector.Mapping.Variables[currToken.Text.ToLower()];
                        (scriptContents, padding) = Helper.ReplaceItem(currToken, replaceWith, scriptContents, padding);
                    }
                }
                else if (Helper.IsString(currToken))
                {
                    string toRewrite = currToken.Text;
                    bool alreadyProcessed = false;
                    if (CommandLineData.PowerShell.Rename.Variables)
                    {
                        if (Helper.IsPSBoundReference(psTokens, i))
                        {
                            string name = toRewrite.Trim('\'', '"');
                            if (DataCollector.Mapping.Variables.ContainsKey(name.ToLower()))
                            {
                                replaceWith = Helper.ReplaceVariableInString(currToken.Text, name, DataCollector.Mapping.Variables[name.ToLower()]);
                                if (replaceWith != "")
                                {
                                    toRewrite = replaceWith;
                                    alreadyProcessed = true;
                                }
                            }
                        }

                        if (Helper.IsAssignment(psTokens, i) && !alreadyProcessed)
                        {
                            string name = currToken.Text.Trim('\'', '"');
                            if (DataCollector.Mapping.Variables.ContainsKey(name.ToLower()))
                            {
                                replaceWith = Helper.ReplaceVariableInString(currToken.Text, name, DataCollector.Mapping.Variables[name.ToLower()]);
                                if (replaceWith != "")
                                {
                                    toRewrite = replaceWith;
                                    alreadyProcessed = true;
                                }
                            }
                        }

                        if (!alreadyProcessed)
                        {
                            string name = currToken.Text.Trim('\'', '"');
                            if (DataCollector.Mapping.Variables.ContainsKey(name.ToLower()))
                            {
                                replaceWith = Helper.ReplaceVariableInString(currToken.Text, "$" + name, DataCollector.Mapping.Variables[name.ToLower()]);
                                if (replaceWith != "")
                                {
                                    toRewrite = replaceWith;
                                    alreadyProcessed = true;
                                }
                            }
                        }
                    }

                    if (CommandLineData.Global.Rewrite.Strings)
                    {
                        if (Helper.CanReplaceString(psTokens, i))
                        {
                            toRewrite = RewriteString(toRewrite);
                        }
                    }
                    (scriptContents, padding) = Helper.ReplaceItem(currToken, toRewrite, scriptContents, padding);
                }
                else if (Helper.IsParameter(currToken))
                {
                    if (!CommandLineData.PowerShell.Rename.Parameters)
                    {
                        continue;
                    }

                    if (Helper.IsCallingFunctionBuiltIn(psTokens, i))
                    {
                        continue;
                    }

                    replaceWith = "-" + DataCollector.Mapping.Variables[currToken.Text.Substring(1).ToLower()];
                    (scriptContents, padding) = Helper.ReplaceItem(currToken, replaceWith, scriptContents, padding);
                }
                else if (Helper.IsSplattedVariable(currToken))
                {
                    string name = currToken.Text.TrimStart('@');
                    if (DataCollector.Mapping.Variables.ContainsKey(name.ToLower()))
                    {
                        (scriptContents, padding) = Helper.ReplaceItem(currToken, '@' + DataCollector.Mapping.Variables[name.ToLower()], scriptContents, padding);
                    }
                }
            }
            Logger.Info("", true, false);

            return scriptContents;
        }

        public string RewriteString(string text)
        {
            string code = "";
            text = text.Trim('\"').Trim('\'');
            switch (CommandLineData.Global.Rewrite.EncodingMethod)
            {
                case StringEncoding.StringEncodingMethods.Base64:
                    text = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
                    code = "([System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String(\"" + text + "\")))";
                    break;
                case StringEncoding.StringEncodingMethods.XorEncrypt:
                    string key = NameGenerator.Generate();
                    text = StringEncoding.Xor(text, key, false, false);
                    code = "(" + CommandLineData.Global.Rewrite.Template.Function + " \"" + text + "\" \"" + key + "\")";
                    break;
                case StringEncoding.StringEncodingMethods.SingleCharacterSubstitution:
                    text = StringEncoding.SingleCharacter(text, CommandLineData.Global.Rewrite.SingleMapping);
                    code = "(" + CommandLineData.Global.Rewrite.Template.Function + " \"" + text + "\")";
                    break;
                case StringEncoding.StringEncodingMethods.GroupCharacterSubstitution:
                    text = StringEncoding.GroupCharacter(text, CommandLineData.Global.Rewrite.GroupMapping);
                    code = "(" + CommandLineData.Global.Rewrite.Template.Function + " \"" + text + "\")";
                    break;
            }
            return code;
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
                    mapping = StringEncoding.ExportSingleCharacterMap(CommandLineData.Global.Rewrite.SingleMapping, ModuleTypes.CodecepticonModules.Powershell);
                    code = code.Replace("%MAPPING%", mapping);
                    break;
                case StringEncoding.StringEncodingMethods.GroupCharacterSubstitution:
                    mapping = StringEncoding.ExportGroupCharacterMap(CommandLineData.Global.Rewrite.GroupMapping, ModuleTypes.CodecepticonModules.Powershell);
                    code = code.Replace("%MAPPING%", mapping);
                    break;
            }

            return code;
        }
    }
}
