using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Tree;
using Codecepticon.Modules.VB6.Collectors;

namespace Codecepticon.Modules.VB6
{
    class DataCollector
    {
        public static List<string> AllIdentifiers = new List<string>();
        public static List<string> AllTypes = new List<string>();
        public static List<string> AllEnums = new List<string>();

        public struct MappingStruct
        {
            public Dictionary<string, string> Identifiers;
        }

        public static MappingStruct Mapping = new MappingStruct
        {
            Identifiers = new Dictionary<string, string>()
        };

        public void CollectData(IParseTree tree)
        {
            TypeCollectorVisitor typeVisitor = new TypeCollectorVisitor();
            CollectorVisitor visitor = new CollectorVisitor();

            // First we need to collect custom types and enums. This way if something is defined as "Worksheet", it won't be replaced.
            ParseTreeWalker.Default.Walk(typeVisitor, tree);
            ParseTreeWalker.Default.Walk(visitor, tree);
            
            AllIdentifiers = AllIdentifiers.Distinct().ToList();
            // We need to remove any keywords that exist in the list but don't exist in types.
            AllIdentifiers.Sort();
        }

        public static bool IsMappingUnique(string name)
        {
            // VB6 is NOT case-sensitive, so everything has to be unique.
            string item = Mapping.Identifiers.FirstOrDefault(s => s.Value.ToLower() == name.ToLower()).Key;
            return item == null;
        }
    }
}
