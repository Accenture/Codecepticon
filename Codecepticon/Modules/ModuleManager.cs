using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.Utils;

namespace Codecepticon.Modules
{
    class ModuleManager
    {
        protected static async Task<string> GenerateName(NameGenerator nameGenerator, Func<string, bool> isMappingUnique)
        {
            string name;
            int duplicateAttempts = 0;
            int attemptLimit = 100;
            do
            {
                name = nameGenerator.Generate();
                if (isMappingUnique(name))
                {
                    break;
                }
            } while (++duplicateAttempts < attemptLimit);

            if (duplicateAttempts >= attemptLimit)
            {
                Logger.Error($"Failed {attemptLimit} times to generate a unique string that is not already a mapping. Increase your character set / length / dictionary, and try again.");
                return "";
            }

            return name;
        }
    }
}
