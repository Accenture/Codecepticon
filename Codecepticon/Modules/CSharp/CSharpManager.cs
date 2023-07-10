using Codecepticon.CommandLine;
using Codecepticon.Utils;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.CSharp
{
    class CSharpManager : ModuleManager
    {
        public async Task Run()
        {
            // https://stackoverflow.com/a/57157323
            string roslynVersion = Assembly.GetAssembly(typeof(Solution)).GetName().Version.ToString();
            Logger.Debug("Identified Roslyn version: " + roslynVersion);

            if (roslynVersion != "3.9.0.0")
            {
                // This has to be here until https://github.com/dotnet/roslyn/issues/58463 is closed.
                Logger.Warning("There is an issue in Roslyn that prevents Codecepticon from working properly: https://github.com/dotnet/roslyn/issues/58463");
                Logger.Warning($"Codecepticon works with v3.9.0.0 but the current Microsoft.CodeAnalysis package installed is {roslynVersion}");
                string userResponse;
                do
                {
                    Logger.Warning("Continue? (Y/N): ", false);
                    userResponse = Console.ReadLine().ToLower();
                } while (userResponse != "y" && userResponse != "n");

                if (userResponse == "n")
                {
                    Logger.Error("Aborted by user");
                    return;
                }
            }

            switch (CommandLineData.Global.Action)
            {
                case CommandLineData.Action.Obfuscate:
                    await Obfuscate();
                    break;
                case CommandLineData.Action.Unmap:
                    await Unmap();
                    break;
            }
        }

        public static async Task Obfuscate()
        {
            Dictionary<string, string> workspaceProperties = new Dictionary<string, string>
            {
                { "Configuration", "Release" }
            };

            using (var workspace = VisualStudioManager.GetWorkspace(workspaceProperties))
            {
                Logger.Info($"Loading solution: {CommandLineData.Global.Project.Path}");
                workspace.WorkspaceFailed += (o, e) => Logger.Debug(e.Diagnostic.Message);
                Solution solution = await workspace.OpenSolutionAsync(CommandLineData.Global.Project.Path);
                Logger.Verbose("Finished loading solution");
                Logger.Verbose("");

                if (CommandLineData.CSharp.Compilation.Precompile)
                {
                    Logger.Info("Pre-compiling the original project to check if it is successful...", false);
                    if (!VisualStudioManager.Build(solution, new Dictionary<string, string> { { "Configuration", "Release" } } ))
                    {
                        if (!Logger.IsDebug)
                        {
                            Logger.Error("ERROR", true, false);
                        }
                        Logger.Error("Could not compile original project in 'Release' mode, make sure there are no errors and try again - or run Codecepticon with --debug.");
                        return;
                    }
                    Logger.Info("OK", true, false);
                }

                if (solution.Projects.Count() > 1)
                {
                    Logger.Warning($"This solution has {solution.Projects.Count()} projects. Codecepticon only supports single-project solutions.");
                    Logger.Warning($"You can still give it a go, but tread carefully - here be dragons");

                    string userResponse;
                    do
                    {
                        Logger.Warning("Continue? (Y/N): ", false);
                        userResponse = Console.ReadLine().ToLower();
                    } while (userResponse != "y" && userResponse != "n");

                    if (userResponse == "n")
                    {
                        Logger.Error("Aborted by user");
                        return;
                    }
                }

                foreach (Project project in solution.Projects)
                {
                    Logger.Info($"Processing project {project.FilePath}");
                    await GatherProjectData(solution, project);
                    await DataCollector.FilterCollectedData();

                    Logger.Verbose("");
                    Logger.Verbose("Elements Found:");
                    Logger.Verbose($"\tNamespaces:\t{DataCollector.AllNamespaces.Count}");
                    Logger.Verbose($"\tClasses:\t{DataCollector.AllClasses.Count}");
                    Logger.Verbose($"\tEnums:\t\t{DataCollector.AllEnums.Count}");
                    Logger.Verbose($"\tFunctions:\t{DataCollector.AllFunctions.Count}");
                    Logger.Verbose($"\tProperties:\t{DataCollector.AllProperties.Count}");
                    Logger.Verbose($"\tParameters:\t{DataCollector.AllParameters.Count}");
                    Logger.Verbose($"\tVariables:\t{DataCollector.AllVariables.Count}");
                    Logger.Verbose($"\tStructs:\t{DataCollector.AllStructs.Count}");

                    Logger.Info("Generating mappings...");
                    if (await GenerateMappings() == false)
                    {
                        Logger.Error("Could not generate mappings.");
                        return;
                    }

                    Logger.Info("Rewriting code...");
                    solution = await RewriteCode(solution, project);
                }

                VisualStudioManager.SetProjectConfiguration(solution, new Dictionary<string, string> { { "Configuration", CommandLineData.CSharp.Compilation.Configuration } });
                VisualStudioManager.SetProjectConfiguration(solution, CommandLineData.CSharp.Compilation.Settings);

                Logger.Info("Applying changes to solution...");
                workspace.TryApplyChanges(solution);

                // At this point, when everything has been applied, we can do any final project-wide updates.
                if (CommandLineData.CSharp.Profile != null)
                {
                    Logger.Info("Running profile-specific final actions...", false);
                    foreach (Project project in solution.Projects)
                    {
                        solution = await CommandLineData.CSharp.Profile.Final(solution, project);
                    }
                    Logger.Info("", true, false);
                    Logger.Info("Applying changes (again) to solution...");
                    workspace.TryApplyChanges(solution);
                }
                
                if (CommandLineData.CSharp.Compilation.Build)
                {
                    Logger.Info("Building solution...", false);
                    if (!VisualStudioManager.Build(solution))
                    {
                        Logger.Error("ERROR", true, false);
                        Logger.Error("The codebase has been obfuscated, but there was an error while building the solution.");
                    }
                    else
                    {
                        Logger.Info("OK", true, false);
                    }
                }
            }

            Logger.Info("Generating mapping file to: " + CommandLineData.Global.Unmap.MapFile);
            Unmapping.GenerateMapFile(CommandLineData.Global.Unmap.MapFile);

            if (CommandLineData.Global.Rewrite.EncodingMethod == StringEncoding.StringEncodingMethods.ExternalFile)
            {
                Logger.Warning($"Make sure you place {CommandLineData.Global.Rewrite.ExternalFile} somewhere where your obfsucated target can find it!");
            }

            Logger.Success("Obfuscation complete");
        }

        public static async Task Unmap()
        {
            Unmapping unmapping = new Unmapping();
            unmapping.Run(CommandLineData.Global.Unmap.MapFile, CommandLineData.Global.Unmap.File, CommandLineData.Global.Unmap.Directory, CommandLineData.Global.Unmap.Recursive);
        }

        public static async Task GatherProjectData(Solution solution, Project project)
        {
            DataCollector.Mapping.CommandLine = new Dictionary<string, DataCollector.CommandLine>();

            foreach (Document document in project.Documents)
            {
                Logger.Debug($"Gathering data for {document.FilePath}");

                await DataCollector.CollectNamespaces(solution, project.Name, document.Name);
                await DataCollector.CollectClasses(solution, project.Name, document.Name);
                await DataCollector.CollectEnums(solution, project.Name, document.Name);
                await DataCollector.CollectFunctions(solution, project.Name, document.Name);
                await DataCollector.CollectProperties(solution, project.Name, document.Name);
                await DataCollector.CollectVariables(solution, project.Name, document.Name);
                await DataCollector.CollectParameters(solution, project.Name, document.Name);
                await DataCollector.CollectStructs(solution, project.Name, document.Name);
            }
        }

        public static async Task<bool> GenerateMappings()
        {
            if (CommandLineData.CSharp.Rename.Enabled)
            {
                CommandLineData.Global.NameGenerator = new NameGenerator(
                    CommandLineData.Global.RenameGenerator.Method,
                    CommandLineData.Global.RenameGenerator.Data.CharacterSet.Value,
                    CommandLineData.Global.RenameGenerator.Data.CharacterSet.Length,
                    CommandLineData.Global.RenameGenerator.Data.Dictionary.Words
                );
            }
            else
            {
                CommandLineData.Global.NameGenerator = new NameGenerator(
                    NameGenerator.RandomNameGeneratorMethods.RandomCombinations,
                    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz",
                    0
                );
            }

            string newName;

            if (CommandLineData.CSharp.Rename.Namespaces)
            {
                Logger.Verbose("Creating mappings for namespaces");
                foreach (string name in DataCollector.AllNamespaces)
                {
                    newName = await GenerateName(CommandLineData.Global.NameGenerator, DataCollector.IsMappingUnique);
                    if (newName.Length == 0)
                    {
                        return false;
                    }

                    DataCollector.Mapping.Namespaces.Add(name, newName);
                }
            }

            if (CommandLineData.CSharp.Rename.Classes)
            {
                Logger.Verbose("Creating mappings for classes");
                foreach (string name in DataCollector.AllClasses)
                {
                    newName = await GenerateName(CommandLineData.Global.NameGenerator, DataCollector.IsMappingUnique);
                    if (newName.Length == 0)
                    {
                        return false;
                    }

                    DataCollector.Mapping.Classes.Add(name, newName);
                }
            }

            if (CommandLineData.CSharp.Rename.Functions)
            {
                Logger.Verbose("Creating mappings for functions");
                foreach (string name in DataCollector.AllFunctions)
                {
                    newName = await GenerateName(CommandLineData.Global.NameGenerator, DataCollector.IsMappingUnique);
                    if (newName.Length == 0)
                    {
                        return false;
                    }

                    DataCollector.Mapping.Functions.Add(name, newName);
                }
            }

            if (CommandLineData.CSharp.Rename.Enums)
            {
                Logger.Verbose("Creating mappings for enums");
                foreach (string name in DataCollector.AllEnums)
                {
                    newName = await GenerateName(CommandLineData.Global.NameGenerator, DataCollector.IsMappingUnique);
                    if (newName.Length == 0)
                    {
                        return false;
                    }

                    DataCollector.Mapping.Enums.Add(name, newName);
                }
            }

            if (CommandLineData.CSharp.Rename.Properties)
            {
                Logger.Verbose("Creating mappings for properties");
                foreach (string name in DataCollector.AllProperties)
                {
                    newName = await GenerateName(CommandLineData.Global.NameGenerator, DataCollector.IsMappingUnique);
                    if (newName.Length == 0)
                    {
                        return false;
                    }

                    DataCollector.Mapping.Properties.Add(name, newName);
                }
            }

            if (CommandLineData.CSharp.Rename.Variables)
            {
                Logger.Verbose("Creating mappings for variables");
                foreach (string name in DataCollector.AllVariables)
                {
                    newName = await GenerateName(CommandLineData.Global.NameGenerator, DataCollector.IsMappingUnique);
                    if (newName.Length == 0)
                    {
                        return false;
                    }

                    DataCollector.Mapping.Variables.Add(name, newName);
                }
            }

            if (CommandLineData.CSharp.Rename.Parameters)
            {
                Logger.Verbose("Creating mappings for parameters");
                foreach (string name in DataCollector.AllParameters)
                {
                    newName = await GenerateName(CommandLineData.Global.NameGenerator, DataCollector.IsMappingUnique);
                    if (newName.Length == 0)
                    {
                        return false;
                    }

                    DataCollector.Mapping.Parameters.Add(name, newName);
                }
            }

            if (CommandLineData.CSharp.Rename.Structs)
            {
                Logger.Verbose("Creating mappings for structs");
                foreach (string name in DataCollector.AllStructs)
                {
                    newName = await GenerateName(CommandLineData.Global.NameGenerator, DataCollector.IsMappingUnique);
                    if (newName.Length == 0)
                    {
                        return false;
                    }

                    DataCollector.Mapping.Structs.Add(name, newName);
                }
            }

            return true;
        }

        public static async Task<Solution> RewriteCode(Solution solution, Project project)
        {
            DataRenamer dataRenamer = new DataRenamer();
            DataRewriter dataRewriter = new DataRewriter();
            int step = 10;

            if (CommandLineData.CSharp.Profile != null)
            {
                Logger.Info("Selected profile is: " + CommandLineData.CSharp.Profile.Name);
                Logger.Info("Running profile-specific pre-process actions...", false);
                solution = await CommandLineData.CSharp.Profile.Before(solution, project);
                Logger.Info("", true, false);
            }

            Logger.Info("Rewriting assemblies...", CommandLineData.Global.Project.Debug);
            int c = 0;
            foreach (Document document in project.Documents)
            {
                if (CommandLineData.Global.Project.Debug)
                {
                    Logger.Debug($"Rewriting assemblies in document {document.FilePath}");
                } else if (++c % step == 0)
                {
                    Logger.Verbose(".", false, false);
                }
                solution = await dataRewriter.RewriteAssemblies(solution, project.Name, document.Name);
            }
            if (!CommandLineData.Global.Project.Debug)
            {
                Logger.Info("", true, false);
            }

            Logger.Info("Removing comments...", CommandLineData.Global.Project.Debug);
            c = 0;
            foreach (Document document in project.Documents)
            {
                if (CommandLineData.Global.Project.Debug)
                {
                    Logger.Debug($"Removing comments in document {document.FilePath}");
                }
                else if (++c % step == 0)
                {
                    Logger.Verbose(".", false, false);
                }
                solution = await dataRewriter.RemoveComments(solution, project.Name, document.Name);
            }
            if (!CommandLineData.Global.Project.Debug)
            {
                Logger.Info("", true, false);
            }

            if (CommandLineData.Global.Rewrite.Strings)
            {
                Logger.Info("Rewriting switch statements...", CommandLineData.Global.Project.Debug);
                c = 0;
                foreach (Document document in project.Documents)
                {
                    if (CommandLineData.Global.Project.Debug)
                    {
                        Logger.Debug($"Rewriting switch statements in document {document.FilePath}");
                    }
                    else if (++c % step == 0)
                    {
                        Logger.Verbose(".", false, false);
                    }
                    solution = await dataRewriter.RewriteSwitchStatements(solution, project.Name, document.Name);
                }
                if (!CommandLineData.Global.Project.Debug)
                {
                    Logger.Info("", true, false);
                }

                Logger.Info("Rewriting strings...", CommandLineData.Global.Project.Debug);
                switch (CommandLineData.Global.Rewrite.EncodingMethod)
                {
                    case StringEncoding.StringEncodingMethods.XorEncrypt:
                    case StringEncoding.StringEncodingMethods.SingleCharacterSubstitution:
                    case StringEncoding.StringEncodingMethods.GroupCharacterSubstitution:
                    case StringEncoding.StringEncodingMethods.ExternalFile:
                        solution = await dataRewriter.AddStringHelperClass(solution, project);
                        break;
                }

                c = 0;
                foreach (Document document in project.Documents)
                {
                    if (CommandLineData.Global.Project.Debug)
                    {
                        Logger.Debug($"Rewriting strings in document {document.FilePath}");
                    }
                    else if (++c % step == 0)
                    {
                        Logger.Verbose(".", false, false);
                    }
                    solution = await dataRewriter.RewriteStrings(solution, project.Name, document.Name);
                }

                // Now save the mapping to file(for some methods).
                switch (CommandLineData.Global.Rewrite.EncodingMethod)
                {
                    case StringEncoding.StringEncodingMethods.ExternalFile:
                        StringEncoding.SaveExportExternalFileMapping(CommandLineData.Global.Rewrite.ExternalFile);
                        break;
                }
                if (!CommandLineData.Global.Project.Debug)
                {
                    Logger.Info("", true, false);
                }
            }

            if (CommandLineData.CSharp.Rename.Namespaces)
            {
                Logger.Info($"Renaming {DataCollector.Mapping.Namespaces.Count} namespaces...", CommandLineData.Global.Project.Debug);
                c = 0;
                foreach (Document document in project.Documents)
                {
                    if (CommandLineData.Global.Project.Debug)
                    {
                        Logger.Debug($"Renaming namespaces in document {document.FilePath}");
                    }
                    else if (++c % step == 0)
                    {
                        Logger.Verbose(".", false, false);
                    }
                    solution = await dataRenamer.RenameNamespaces(solution, project.Name, document.Name);
                }
                if (!CommandLineData.Global.Project.Debug)
                {
                    Logger.Info("", true, false);
                }
            }

            if (CommandLineData.CSharp.Rename.Classes)
            {
                Logger.Info($"Renaming {DataCollector.Mapping.Classes.Count} classes...", CommandLineData.Global.Project.Debug);
                c = 0;
                foreach (Document document in project.Documents)
                {
                    if (CommandLineData.Global.Project.Debug)
                    {
                        Logger.Debug($"Renaming classes in document {document.FilePath}");
                    }
                    else if (++c % step == 0)
                    {
                        Logger.Verbose(".", false, false);
                    }
                    solution = await dataRenamer.RenameClasses(solution, project.Name, document.Name);
                }
                if (!CommandLineData.Global.Project.Debug)
                {
                    Logger.Info("", true, false);
                }
            }

            if (CommandLineData.CSharp.Rename.Enums)
            {
                Logger.Info($"Renaming {DataCollector.Mapping.Enums.Count} enums...", CommandLineData.Global.Project.Debug);
                c = 0;
                foreach (Document document in project.Documents)
                {
                    if (CommandLineData.Global.Project.Debug)
                    {
                        Logger.Debug($"Renaming enums in document {document.FilePath}");
                    }
                    else if (++c % step == 0)
                    {
                        Logger.Verbose(".", false, false);
                    }
                    solution = await dataRenamer.RenameEnums(solution, project.Name, document.Name);
                }
                if (!CommandLineData.Global.Project.Debug)
                {
                    Logger.Info("", true, false);
                }
            }

            if (CommandLineData.CSharp.Rename.Functions)
            {
                Logger.Info($"Renaming {DataCollector.Mapping.Functions.Count} functions...", CommandLineData.Global.Project.Debug);
                c = 0;
                foreach (Document document in project.Documents)
                {
                    if (CommandLineData.Global.Project.Debug)
                    {
                        Logger.Debug($"Renaming functions in document {document.FilePath}");
                    }
                    else if (++c % step == 0)
                    {
                        Logger.Verbose(".", false, false);
                    }
                    solution = await dataRenamer.RenameFunctions(solution, project.Name, document.Name);
                }
                if (!CommandLineData.Global.Project.Debug)
                {
                    Logger.Info("", true, false);
                }
            }

            if (CommandLineData.CSharp.Rename.Properties)
            {
                Logger.Info($"Renaming {DataCollector.Mapping.Properties.Count} properties...", CommandLineData.Global.Project.Debug);
                c = 0;
                foreach (Document document in project.Documents)
                {
                    if (CommandLineData.Global.Project.Debug)
                    {
                        Logger.Debug($"Renaming properties in document {document.FilePath}");
                    }
                    else if (++c % step == 0)
                    {
                        Logger.Verbose(".", false, false);
                    }
                    solution = await dataRenamer.RenameProperties(solution, project.Name, document.Name);
                }
                if (!CommandLineData.Global.Project.Debug)
                {
                    Logger.Info("", true, false);
                }
            }

            if (CommandLineData.CSharp.Rename.Parameters)
            {
                Logger.Info($"Renaming {DataCollector.Mapping.Parameters.Count} parameters...", CommandLineData.Global.Project.Debug);
                c = 0;
                foreach (Document document in project.Documents)
                {
                    if (CommandLineData.Global.Project.Debug)
                    {
                        Logger.Debug($"Renaming parameters in document {document.FilePath}");
                    }
                    else if (++c % step == 0)
                    {
                        Logger.Verbose(".", false, false);
                    }
                    solution = await dataRenamer.RenameParameters(solution, project.Name, document.Name);
                }
                if (!CommandLineData.Global.Project.Debug)
                {
                    Logger.Info("", true, false);
                }
            }

            if (CommandLineData.CSharp.Rename.Variables)
            {
                Logger.Info($"Renaming {DataCollector.Mapping.Variables.Count} variables...", CommandLineData.Global.Project.Debug);
                c = 0;
                foreach (Document document in project.Documents)
                {
                    if (CommandLineData.Global.Project.Debug)
                    {
                        Logger.Debug($"Renaming variables in document {document.FilePath}");
                    }
                    else if (++c % step == 0)
                    {
                        Logger.Verbose(".", false, false);
                    }
                    solution = await dataRenamer.RenameVariables(solution, project.Name, document.Name);
                }
                if (!CommandLineData.Global.Project.Debug)
                {
                    Logger.Info("", true, false);
                }
            }

            if (CommandLineData.CSharp.Rename.Structs)
            {
                Logger.Info($"Renaming {DataCollector.Mapping.Structs.Count} structs...", CommandLineData.Global.Project.Debug);
                c = 0;
                foreach (Document document in project.Documents)
                {
                    if (CommandLineData.Global.Project.Debug)
                    {
                        Logger.Debug($"Renaming structs in document {document.FilePath}");
                    }
                    else if (++c % step == 0)
                    {
                        Logger.Verbose(".", false, false);
                    }
                    solution = await dataRenamer.RenameStructs(solution, project.Name, document.Name);
                }
                if (!CommandLineData.Global.Project.Debug)
                {
                    Logger.Info("", true, false);
                }
            }

            Logger.Debug($"Setting ProjectGuid to {{{CommandLineData.Global.Project.Guid.ToString().ToUpper()}}}");
            VisualStudioManager.SetProjectConfiguration(solution, new Dictionary<string, string>
            {
                { "ProjectGuid", $"{{{CommandLineData.Global.Project.Guid.ToString().ToUpper()}}}" },
                { "ApplicationIcon", "" },
            });

            if (CommandLineData.CSharp.Profile != null)
            {
                Logger.Info("Running profile-specific post-process actions...", false);
                solution = await CommandLineData.CSharp.Profile.After(solution, project);
                Logger.Info("", true, false);
            }

            return solution;
        }
    }
}
