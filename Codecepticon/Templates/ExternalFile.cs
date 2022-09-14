using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace %_NAMESPACE_%
{
    class %_CLASS_%
    {
        public static List<string> StringMapping = new List<string>();
        
        public static string %_FUNCTION_%(int index)
        {
            if (StringMapping.Count == 0) {
                StringMapping = File.ReadAllLines("%MAPPING%").ToList().Select(v => Encoding.UTF8.GetString(Convert.FromBase64String(v))).ToList();
            }
            
            return StringMapping[index];
        }
    }
}
