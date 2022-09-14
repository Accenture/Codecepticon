using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.PowerShell
{
    class DataCollector
    {
        public static SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public static List<string> AllFunctions = new List<string>();
        public static List<string> AllVariables = new List<string>();

        public struct MappingStruct
        {
            public Dictionary<string, string> Functions;
            public Dictionary<string, string> Variables;
        }

        public static MappingStruct Mapping = new MappingStruct
        {
            Functions = new Dictionary<string, string>(),
            Variables = new Dictionary<string, string>(),
        };

        public static bool IsMappingUnique(string name)
        {
            string item = Mapping.Functions.FirstOrDefault(s => s.Value == name.ToLower()).Key;
            if (item != null)
            {
                return false;
            }

            item = Mapping.Variables.FirstOrDefault(s => s.Value == name.ToLower()).Key;
            return item == null;
        }

        public static async Task CollectFunctions(Token[] psTokens)
        {
            for (int i = 0; i < psTokens.Length; i++)
            {
                Token prevToken = (i == 0) ? null : psTokens[i - 1];
                Token currToken = psTokens[i];

                if (Helper.IsFunction(prevToken))
                {
                    // Using .ToLower() because PowerShell isn't case-sensitive.
                    if (!currToken.Text.Contains(":") && !AllFunctions.Contains(currToken.Text.ToLower()))
                    {
                        AllFunctions.Add(currToken.Text.ToLower());
                    }
                }
            }
            AllFunctions.Sort();
        }

        public static async Task CollectVariables(Token[] psTokens)
        {
            foreach (Token currToken in psTokens)
            {
                if (Helper.IsVariable(currToken))
                {
                    // Using .ToLower() because PowerShell isn't case-sensitive.
                    string name = currToken.Text.Substring(1).ToLower(); // Remove the $ from the beginning.
                    if (!name.Contains(":") && !AllVariables.Contains(name))
                    {
                        AllVariables.Add(name);
                    }
                }
            }
            AllVariables.Sort();
        }

        public static async Task CollectParameters(Token[] psTokens)
        {
            for (int i = 0; i < psTokens.Length; i++)
            {
                Token currToken = psTokens[i];

                if (Helper.IsParameter(currToken))
                {
                    if (Helper.IsCallingFunctionBuiltIn(psTokens, i))
                    {
                        continue;
                    }

                    // Using .ToLower() because PowerShell isn't case-sensitive.
                    string name = currToken.Text.Substring(1).ToLower(); // Remove the $ from the beginning.
                    if (!name.Contains(":") && !AllVariables.Contains(name))
                    {
                        AllVariables.Add(name);
                    }
                }
            }
            AllVariables.Sort();
        }
    }
}
