using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Utils;
using Codecepticon.Utils.MarkovWordGenerator;

namespace Codecepticon.Modules
{
    class CommandLineValidator
    {
        protected Templates templateManager = new Templates();

        protected bool ValidateRewrite(ModuleTypes.CodecepticonModules module)
        {
            if (!CommandLineData.Global.Rewrite.Strings)
            {
                return true;
            }

            Templates templateManager = new Templates();

            // Check string rewrite requirements.
            CommandLineData.Global.Rewrite.Template.File = "";
            switch (CommandLineData.Global.Rewrite.EncodingMethod)
            {
                case StringEncoding.StringEncodingMethods.Base64:
                    if (module == ModuleTypes.CodecepticonModules.Vb6)
                    {
                        CommandLineData.Global.Rewrite.Template.File = templateManager.GetTemplateFile(module, StringEncoding.StringEncodingMethods.Base64);
                    }
                    break;
                case StringEncoding.StringEncodingMethods.XorEncrypt:
                    CommandLineData.Global.Rewrite.Template.File = templateManager.GetTemplateFile(module, StringEncoding.StringEncodingMethods.XorEncrypt);
                    break;
                case StringEncoding.StringEncodingMethods.SingleCharacterSubstitution:
                    // Only change alphabet and digits, no special chars.
                    CommandLineData.Global.Rewrite.Template.File = templateManager.GetTemplateFile(module, StringEncoding.StringEncodingMethods.SingleCharacterSubstitution);
                    CommandLineData.Global.Rewrite.SingleMapping = StringEncoding.GenerateSingleCharacterMap("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 !.,-_[](){}:/+");
                    break;
                case StringEncoding.StringEncodingMethods.GroupCharacterSubstitution:
                    // Requires character set, length is optional.
                    if (String.IsNullOrEmpty(CommandLineData.Global.Rewrite.CharacterSet.Value))
                    {
                        Logger.Error("'string-rewrite-charset' cannot be empty when you are using group character name generation.");
                        return false;
                    }
                    else if (CommandLineData.Global.Rewrite.CharacterSet.Length <= 0)
                    {
                        Logger.Error("'string-rewrite-length' cannot be zero when you are using group character name generation.");
                        return false;
                    }

                    CommandLineData.Global.Rewrite.Template.File = templateManager.GetTemplateFile(module, StringEncoding.StringEncodingMethods.GroupCharacterSubstitution);
                    CommandLineData.Global.Rewrite.GroupMapping = StringEncoding.GenerateGroupCharacterMap(CommandLineData.Global.Rewrite.CharacterSet.Value, CommandLineData.Global.Rewrite.CharacterSet.Length);
                    if (!CommandLineData.Global.Rewrite.GroupMapping.Any())
                    {
                        Logger.Error("Could not generate group substitution mapping. Ensure your character set and length are large enough to accommodate 256 variations.");
                        return false;
                    }

                    break;
                case StringEncoding.StringEncodingMethods.ExternalFile:
                    if (String.IsNullOrEmpty(CommandLineData.Global.Rewrite.ExternalFile))
                    {
                        Logger.Error("'string-rewrite-extfile' is empty.");
                        return false;
                    }

                    CommandLineData.Global.Rewrite.Template.File = templateManager.GetTemplateFile(module, StringEncoding.StringEncodingMethods.ExternalFile);
                    break;
                case StringEncoding.StringEncodingMethods.Unknown:
                    Logger.Error("'string-rewrite-method' is missing or is not valid.");
                    return false;
            }

            if (CommandLineData.Global.Rewrite.Template.File.Length > 0 && !File.Exists(CommandLineData.Global.Rewrite.Template.File))
            {
                Logger.Error("Template file does not exist: " + CommandLineData.Global.Rewrite.Template.File);
                return false;
            }

            return true;
        }

        protected bool ValidateActionUnmap()
        {
            if (String.IsNullOrEmpty(CommandLineData.Global.Unmap.MapFile) || !File.Exists(CommandLineData.Global.Unmap.MapFile))
            {
                Logger.Error("'map-file' file does not exist: " + CommandLineData.Global.Unmap.MapFile);
                return false;
            }

            if (String.IsNullOrEmpty(CommandLineData.Global.Unmap.Directory) && String.IsNullOrEmpty(CommandLineData.Global.Unmap.File))
            {
                Logger.Error("Please set either unmap-directory or unmap-file");
                return false;
            }

            if (CommandLineData.Global.Unmap.File != "" && CommandLineData.Global.Unmap.Directory != "")
            {
                Logger.Error("Both 'unmap-file' and 'unmap-directory' have been set - please only choose one.");
                return false;
            }

            if (CommandLineData.Global.Unmap.Directory != "")
            {
                if (!Directory.Exists(CommandLineData.Global.Unmap.Directory))
                {
                    Logger.Error("'unmap-directory' does not exist: " + CommandLineData.Global.Unmap.Directory);
                    return false;
                }
            }
            else if (CommandLineData.Global.Unmap.File != "")
            {
                if (!File.Exists(CommandLineData.Global.Unmap.File))
                {
                    Logger.Error("'unmap-file' does not exist: " + CommandLineData.Global.Unmap.File);
                    return false;
                }
            }

            return true;
        }

        protected bool ValidateMappingFiles()
        {
            if (String.IsNullOrEmpty(CommandLineData.Global.Unmap.MapFile))
            {
                CommandLineData.Global.Unmap.MapFile = CommandLineData.Global.Project.Path + ".html";
            }

            return true;
        }

        protected bool ValidateRename()
        {
            // Check if the right combination of arguments for name generation has been passed.
            switch (CommandLineData.Global.RenameGenerator.Method)
            {
                case NameGenerator.RandomNameGeneratorMethods.RandomCombinations:
                case NameGenerator.RandomNameGeneratorMethods.AvoidTwinCharacters:
                    if (String.IsNullOrEmpty(CommandLineData.Global.RenameGenerator.Data.CharacterSet.Value))
                    {
                        Logger.Error("'rename-charset' cannot be empty when you are using random name generation.");
                        return false;
                    }

                    if (CommandLineData.Global.RenameGenerator.Data.CharacterSet.Value.Any(c => !Char.IsLetter(c)))
                    {
                        Logger.Error("'rename-charset' can only contain letters.");
                        return false;
                    }

                    if (CommandLineData.Global.RenameGenerator.Data.CharacterSet.Length < 0)
                    {
                        Logger.Error("'rename-length' has not been defined.");
                        return false;
                    }

                    break;
                case NameGenerator.RandomNameGeneratorMethods.DictionaryWords:
                    if (!CommandLineData.Global.RenameGenerator.Data.Dictionary.Words.Any())
                    {
                        Logger.Error("No dictionary words or file has been specified.");
                        return false;
                    }
                    break;
                case NameGenerator.RandomNameGeneratorMethods.Markov:
                    if (CommandLineData.Global.Markov.MinLength <= 0)
                    {
                        Logger.Error("'markov-min-length' is invalid");
                        return false;
                    } else if (CommandLineData.Global.Markov.MaxLength <= 0)
                    {
                        Logger.Error("'markov-max-length' is invalid");
                        return false;
                    } else if (CommandLineData.Global.Markov.MinLength > CommandLineData.Global.Markov.MaxLength)
                    {
                        Logger.Error("'markov-min-length' cannot be greater than 'markov-max-length'");
                        return false;
                    }

                    if (CommandLineData.Global.Markov.MinWords <= 0)
                    {
                        Logger.Error("'markov-min-words' is invalid");
                        return false;
                    }
                    else if (CommandLineData.Global.Markov.MaxWords <= 0)
                    {
                        Logger.Error("'markov-max-words' is invalid");
                        return false;
                    }
                    else if (CommandLineData.Global.Markov.MinWords > CommandLineData.Global.Markov.MaxWords)
                    {
                        Logger.Error("'markov-min-words' cannot be greater than 'markov-max-words'");
                        return false;
                    }

                    // We need to train the model if it all went well.
                    Logger.Verbose("Training Markov Generator...");
                    TrainMarkovGenerator(templateManager.GetTemplateFile("markov-english-words.txt"));
                    break;
                default:
                    Logger.Error("Invalid rename generation method set.");
                    return false;
            }

            return true;
        }

        protected void TrainMarkovGenerator(string dictionary)
        {
            CommandLineData.Global.Markov.Generator = new MarkovWordGenerator(dictionary, 3, 0.01);
        }
    }
}
