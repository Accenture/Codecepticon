using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Utils;

namespace Codecepticon.Modules.PowerShell
{
    class PowerShellManager : ModuleManager
    {
        public async Task Run()
        {

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

        public static async Task<string> RewriteCode(string script)
        {
            DataRenamer dataRenamer = new DataRenamer();
            Parser.ParseInput(script, out Token[] psTokens, out ParseError[] psErrors);

            string stringDecoding = "";
            if (CommandLineData.Global.Rewrite.Strings)
            {
                switch (CommandLineData.Global.Rewrite.EncodingMethod)
                {
                    case StringEncoding.StringEncodingMethods.XorEncrypt:
                    case StringEncoding.StringEncodingMethods.SingleCharacterSubstitution:
                    case StringEncoding.StringEncodingMethods.GroupCharacterSubstitution:
                        stringDecoding = dataRenamer.AddStringHelperFunction();
                        break;
                }
            }

            script = await dataRenamer.Rename(script, psTokens);

            if (stringDecoding.Length > 0)
            {
                script = stringDecoding + "\r\n" + script.Trim();
            }

            return script;
        }

        public static async Task<string> ExpandStrings(string script)
        {
            DataRenamer dataRenamer = new DataRenamer();
            ParseError[] psErrors;
            Token[] psTokens;

            // First we expand strings.
            Parser.ParseInput(script, out psTokens, out psErrors);
            script = await dataRenamer.ExpandStrings(script, psTokens);

            // Then, we extract special characters like `r and `n because those won't survice a B64 encode/decode.
            Parser.ParseInput(script, out psTokens, out psErrors);
            script = await dataRenamer.ExpandSpecialCharacters(script, psTokens);

            return script;
        }

        public static async Task GatherProjectData(string script)
        {
            Parser.ParseInput(script, out Token[] psTokens, out ParseError[] psErrors);

            await DataCollector.CollectFunctions(psTokens);
            await DataCollector.CollectVariables(psTokens);
            await DataCollector.CollectParameters(psTokens);
        }

        public static async Task<bool> GenerateMappings()
        {
            CommandLineData.Global.NameGenerator = new NameGenerator(
                CommandLineData.Global.RenameGenerator.Method,
                CommandLineData.Global.RenameGenerator.Data.CharacterSet.Value,
                CommandLineData.Global.RenameGenerator.Data.CharacterSet.Length,
                CommandLineData.Global.RenameGenerator.Data.Dictionary.Words
            );

            // First, sort everything by string length. This is done because some variables are contained within
            // strings, and the easiest way to identify variables in strings is to loop through them and see
            // if there are any matches. But if we have a variable called "$Hi" and another "$HiThere", if "Hi"
            // is matched first, it will break the second one. By sorting all lists by length, we eliminate
            // that issue.
            DataCollector.AllFunctions = DataCollector.AllFunctions.OrderByDescending(v => v.Length).ToList();
            DataCollector.AllVariables = DataCollector.AllVariables.OrderByDescending(v => v.Length).ToList();

            string newName;

            if (CommandLineData.PowerShell.Rename.Functions)
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

            if (CommandLineData.PowerShell.Rename.Variables)
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

            return true;
        }

        public static async Task Obfuscate()
        {
            Logger.Info($"Reading script {CommandLineData.Global.Project.Path}");
            string script = File.ReadAllText(CommandLineData.Global.Project.Path);

            Logger.Info("Checking script for errors...");
            var scriptAST = Parser.ParseInput(script, out Token[] psTokens, out ParseError[] psErrors);

            if (psErrors.Length > 0)
            {
                Logger.Error("Could not parse PowerShell script due to syntax errors - ensure script is working before trying to obfuscate it.");
                return;
            }

            Logger.Info("Gathering information...");
            await GatherProjectData(script);
            
            Logger.Verbose("");
            Logger.Verbose("Elements Found:");
            Logger.Verbose($"\tFunctions:\t{DataCollector.AllFunctions.Count}");
            Logger.Verbose($"\tVariables:\t{DataCollector.AllVariables.Count}");

            Logger.Info("Generating mappings...");
            if (await GenerateMappings() == false)
            {
                Logger.Error("Could not generate mappings.");
                return;
            }

            // Expand any variables that exist within strings.
            Logger.Info("Expanding strings...", false);
            script = await ExpandStrings(script);
            Logger.Info("", true, false);

            Logger.Info("Rewriting code...", false);
            script = await RewriteCode(script);

            Logger.Info("Checking script for errors...");
            scriptAST = Parser.ParseInput(script, out psTokens, out psErrors);
            if (psErrors.Length > 0)
            {
                Logger.Error("The obfuscated script contains errors - Obfuscation unsuccessful.");
                //return;
            }

            File.WriteAllText(CommandLineData.Global.Project.SaveAs, script.Trim());

            Logger.Info("Generating mapping file to: " + CommandLineData.Global.Unmap.MapFile);
            Unmapping.GenerateMapFile(CommandLineData.Global.Unmap.MapFile);
            Logger.Success("Obfuscation complete");
        }

        public static async Task Unmap()
        {
            Unmapping unmapping = new Unmapping();
            unmapping.Run(CommandLineData.Global.Unmap.MapFile, CommandLineData.Global.Unmap.File, CommandLineData.Global.Unmap.Directory, CommandLineData.Global.Unmap.Recursive);
        }
    }
}
