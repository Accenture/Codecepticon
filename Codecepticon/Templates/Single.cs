using System.Collections.Generic;

namespace %_NAMESPACE_%
{
    class %_CLASS_%
    {
        public static Dictionary<string, string> mapping = new Dictionary<string, string>() {
            %MAPPING%
        };
        
        public static string %_FUNCTION_%(string text)
        {
            string output = "";
            foreach (char c in text)
            {
                if (mapping.ContainsKey(c.ToString()))
                {
                    output += mapping[c.ToString()];
                }
                else
                {
                    output += c.ToString();
                }
            }

            return output;
        }
    }
}
