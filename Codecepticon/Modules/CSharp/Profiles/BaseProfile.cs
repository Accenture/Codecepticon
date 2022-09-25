using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Utils;
using Microsoft.CodeAnalysis;
using BuildEvaluation = Microsoft.Build.Evaluation;

namespace Codecepticon.Modules.CSharp.Profiles
{
    class BaseProfile
    {
        public virtual string Name { get; } = "Not Set";

        public virtual async Task<Solution> Before(Solution solution, Project project)
        {
            Logger.Debug("Profile does not implement Before function");
            return solution;
        }

        public virtual async Task<Solution> After(Solution solution, Project project)
        {
            BuildEvaluation.Project buildProject = VisualStudioManager.GetBuildProject(project);

            if (CommandLineData.CSharp.Rename.Namespaces || CommandLineData.CSharp.Rename.Classes)
            {
                string startupObject = buildProject.GetPropertyValue("StartupObject");
                if (!String.IsNullOrEmpty(startupObject))
                {
                    Logger.Debug($"StartUp Object was: {startupObject}");
                    string[] elements = startupObject.Split('.');
                    if (elements.Length == 2)
                    {
                        string namespaceValue = CommandLineData.CSharp.Rename.Namespaces && DataCollector.Mapping.Namespaces.ContainsKey(elements[0]) ? DataCollector.Mapping.Namespaces[elements[0]] : elements[0];
                        string classValue = CommandLineData.CSharp.Rename.Classes && DataCollector.Mapping.Classes.ContainsKey(elements[1]) ? DataCollector.Mapping.Classes[elements[1]] : elements[1];
                        Logger.Debug($"StartUp Object is: {namespaceValue}.{classValue}");
                        buildProject.SetProperty("StartupObject", $"{namespaceValue}.{classValue}");
                        buildProject.Save();
                    }
                }
            }

            return solution;
        }

        public virtual async Task<Solution> Final(Solution solution, Project project)
        {
            Logger.Debug("Profile does not implement Final function");
            return solution;
        }

        public virtual bool ValidateCommandLine()
        {
            Logger.Debug("Profile does not implement ValidateCommandLine function");
            return true;
        }
    }
}
