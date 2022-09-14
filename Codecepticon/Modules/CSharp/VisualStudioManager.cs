using Codecepticon.Utils;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuildEvaluation = Microsoft.Build.Evaluation;
using Newtonsoft.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Codecepticon.CommandLine;

namespace Codecepticon.Modules.CSharp
{
    class VisualStudioManager
    {
        public static MSBuildWorkspace GetWorkspace()
        {
            return GetWorkspace(new Dictionary<string, string>());
        }

        public static MSBuildWorkspace GetWorkspace(Dictionary<string, string> properties)
        {
            Logger.Verbose("Getting Visual Studio Instance");
            var instance = GetVisualStudioInstance();
            Logger.Debug($"Using MSBuild at '{instance.MSBuildPath}' - v{instance.Version} to load projects.");

            MSBuildLocator.RegisterInstance(instance);

            Logger.Verbose("Creating MSBuild Workspace");
            return MSBuildWorkspace.Create(properties);
        }

        private static VisualStudioInstance GetVisualStudioInstance()
        {
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            return visualStudioInstances.Length == 1 ? visualStudioInstances[0] : SelectVisualStudioInstance(visualStudioInstances);
        }

        private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Logger.Info("");
            Logger.Info("Multiple installs of MSBuild detected, please select one:");
            Logger.Info("");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Logger.Info($"\t[{i + 1}]\t{visualStudioInstances[i].Name} v{visualStudioInstances[i].Version}");
                Logger.Info($"\t\t{visualStudioInstances[i].MSBuildPath}");
            }
            Logger.Info("");

            while (true)
            {
                Logger.Info("Please enter the number of your selection: ", false);
                var userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out int instanceNumber) && instanceNumber > 0 && instanceNumber <= visualStudioInstances.Length)
                {
                    return visualStudioInstances[instanceNumber - 1];
                }
                Logger.Error("Invalid choice, please try again or use Ctrl-C to quit");
            }
        }

        public static (Project, Document) GetProjectAndDocumentByName(Solution solution, string projectName, string documentName)
        {
            Project project = GetProjectByName(solution, projectName);
            Document document = GetDocumentByName(project, documentName);
            return (project, document);
        }

        public static Project GetProjectByName(Solution solution, string projectName)
        {
            return solution.Projects.FirstOrDefault(s => s.Name == projectName);
        }

        public static Document GetDocumentByName(Project project, string documentName)
        {
            return project.Documents.FirstOrDefault(s => s.Name == documentName);
        }

        public static Document GetDocumentByName(Solution solution, string projectName, string documentName)
        {
            Project project = GetProjectByName(solution, projectName);
            return GetDocumentByName(project, documentName);
        }

        protected static void SetConfiguration(Solution solution, Dictionary<string, string> properties, bool isGlobal)
        {
            Logger.Debug($"VisualStudio SetConfiguration - Global is {isGlobal}");
            Logger.Debug(JsonConvert.SerializeObject(properties));
            foreach (Project project in solution.Projects)
            {
                BuildEvaluation.Project buildProject = GetBuildProject(project);
                SetConfiguration(buildProject, properties, isGlobal);
            }
        }

        protected static void SetConfiguration(BuildEvaluation.Project buildProject, Dictionary<string, string> properties, bool isGlobal)
        {
            foreach (KeyValuePair<string, string> property in properties)
            {
                if (isGlobal)
                {
                    buildProject.SetGlobalProperty(property.Key, property.Value);
                }
                else
                {
                    buildProject.SetProperty(property.Key, property.Value);
                }
            }
            buildProject.Save();
        }

        public static void SetProjectConfiguration(Solution solution, Dictionary<string, string> properties)
        {
            SetConfiguration(solution, properties, false);
        }

        public static BuildEvaluation.Project GetBuildProject(Project project)
        {
            BuildEvaluation.ProjectCollection projectCollection = new BuildEvaluation.ProjectCollection();
            return projectCollection.LoadProject(project.FilePath);
        }

        public static bool Build(Solution solution)
        {
            return Build(solution, new Dictionary<string, string>());
        }

        public static bool Build(Solution solution, Dictionary<string, string> properties)
        {
            Logger.Debug("");
            Logger.Debug("VisualStudio Build");
            Logger.Debug(JsonConvert.SerializeObject(properties));
            try
            {
                bool result = true;
                foreach (Project project in solution.Projects)
                {
                    BuildEvaluation.Project buildProject = GetBuildProject(project);
                    SetConfiguration(buildProject, properties, true);
                    if (CommandLineData.Global.Project.Debug)
                    {
                        result = result && buildProject.Build(new ConsoleLogger());
                    } else
                    {
                        result = result && buildProject.Build();
                    }
                }

                return result;
            } catch (Exception e)
            {
                Logger.Error("ERROR", true, false);
                Logger.Error("Could not compile solution: " + e.Message);
            }
            return false;
        }
    }
}
