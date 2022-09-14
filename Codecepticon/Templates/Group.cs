using System.Collections.Generic;
using System.Linq;

namespace %_NAMESPACE_%
{
    class %_CLASS_%
    {
        public static Dictionary<string, int> mapping = new Dictionary<string, int>() {
            %MAPPING%
        };

        public static string %_FUNCTION_%(string text)
        {
            string output = "";
            int mapLength = mapping.First().Key.Length;
            for (int i = 0; i < text.Length; i += mapLength)
            {
                output += (char)mapping[text.Substring(i, mapLength)];
            }
            return output;
        }
    }
}
