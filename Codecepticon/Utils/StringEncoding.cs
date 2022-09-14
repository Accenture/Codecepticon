using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codecepticon.CommandLine;
using Codecepticon.Modules;

namespace Codecepticon.Utils
{
    class StringEncoding
    {
        public enum StringEncodingMethods
        {
            Unknown = 0,
            Base64 = 1,
            XorEncrypt = 2,
            GroupCharacterSubstitution = 3,
            SingleCharacterSubstitution = 4,
            ExternalFile = 5
        }

        protected static List<string> ExternalFileMapping = new List<string>();

        public static int GetExternalFileIndex(string text)
        {
            int index = ExternalFileMapping.IndexOf(text);
            if (index == -1)
            {
                ExternalFileMapping.Add(text);
                index = ExternalFileMapping.Count - 1;
            }

            return index;
        }

        public static string Xor(string text, string key, bool isTextBase64, bool isKeyBase64)
        {
            byte[] data = isTextBase64 ? Convert.FromBase64String(text) : Encoding.UTF8.GetBytes(text);
            byte[] k = isKeyBase64 ? Convert.FromBase64String(key) : Encoding.UTF8.GetBytes(key);

            byte[] output = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                output[i] = (byte)(data[i] ^ k[i % k.Length]);
            }
            return Convert.ToBase64String(output);
        }

        public static Dictionary<string, string> GenerateSingleCharacterMap(string input)
        {
            Dictionary<string, string> mapping = new Dictionary<string, string>();
            Random randomSeed = new Random();

            string replaceWith = input;

            foreach (char c in input)
            {
                mapping[c.ToString()] = replaceWith[randomSeed.Next(replaceWith.Length)].ToString();
                replaceWith = replaceWith.Replace(mapping[c.ToString()], "");
            }

            return mapping;
        }

        public static Dictionary<string, int> GenerateGroupCharacterMap(string characterSet, int length)
        {
            Dictionary<string, int> mapping = new Dictionary<string, int>();
            char[] uniqueCharacters = characterSet.Distinct().ToArray();
            if (Math.Pow(uniqueCharacters.Length, length) < 256)
            {
                // Will be empty.
                return mapping;
            }

            Random randomSeed = new Random();
            int attemptLimit = 100;
            for (int i = 0; i <= 255; i++)
            {
                int attemptCount = 0;
                string seed;
                do
                {
                    seed = "";
                    for (int k = 0; k < length; k++)
                    {
                        seed += uniqueCharacters[randomSeed.Next(uniqueCharacters.Length - 1)].ToString();
                    }

                    if (!mapping.ContainsKey(seed))
                    {
                        break;
                    }
                } while (++attemptCount < attemptLimit);

                if (attemptCount >= attemptLimit)
                {
                    // Clear and return it empty, to indicate that it wasn't generated.
                    mapping.Clear();
                    break;
                }

                mapping.Add(seed, i);
            }

            return mapping;
        }

        public static string SingleCharacter(string text, Dictionary<string, string> mapping)
        {
            string output = "";
            foreach (char c in text)
            {
                switch (c.ToString())
                {
                    case "\r":
                        output += "\\r";
                        break;
                    case "\n":
                        output += "\\n";
                        break;
                    case "\t":
                        output += "\\t";
                        break;
                    case "\"":
                        output += "\\\"";
                        break;
                    case "\\":
                        output += "\\\\";
                        break;
                    default:
                        output += mapping.ContainsKey(c.ToString()) ? mapping[c.ToString()] : c.ToString();
                        break;
                }
            }

            return output;
        }

        public static string GroupCharacter(string text, Dictionary<string, int> mapping)
        {
            string output = "";
            foreach (byte b in Encoding.UTF8.GetBytes(text.ToCharArray()))
            {
                output += mapping.First(x => x.Value == b).Key;
            }
            return output;
        }

        public static string ExportSingleCharacterMap(Dictionary<string, string> mapping, ModuleTypes.CodecepticonModules module)
        {
            return module switch
            {
                ModuleTypes.CodecepticonModules.CSharp => ExportSingleCharacterMapCSharp(mapping),
                ModuleTypes.CodecepticonModules.Powershell => ExportSingleCharacterMapPowerShell(mapping),
                ModuleTypes.CodecepticonModules.Vb6 => ExportSingleCharacterMapVB6(mapping),
                _ => ""
            };
        }

        protected static string ExportSingleCharacterMapVB6(Dictionary<string, string> mapping)
        {
            string output = "";
            foreach (KeyValuePair<string, string> map in mapping)
            {
                if (output.Length > 0)
                {
                    output += "\r\n";
                }

                output += CommandLineData.Global.Rewrite.Template.Mapping + $".Add \"{map.Value}\", \"{map.Key}\"";
            }
            return output.Trim();
        }

        protected static string ExportSingleCharacterMapPowerShell(Dictionary<string, string> mapping)
        {
            string output = "";
            foreach (KeyValuePair<string, string> map in mapping)
            {
                if (output.Length > 0)
                {
                    output += "; ";
                }

                output += "$" + CommandLineData.Global.Rewrite.Template.Mapping + $"['{map.Value}'] = \"{map.Key}\"";
            }
            return output.Trim();
        }

        protected static string ExportSingleCharacterMapCSharp(Dictionary<string, string> mapping)
        {
            string output = "";
            foreach (KeyValuePair<string, string> map in mapping)
            {
                if (output.Length > 0)
                {
                    output += ", ";
                }
                output += $"{{ \"{map.Value}\", \"{map.Key}\" }}";
            }

            return output;
        }

        public static string ExportGroupCharacterMap(Dictionary<string, int> mapping, ModuleTypes.CodecepticonModules module)
        {
            return module switch
            {
                ModuleTypes.CodecepticonModules.CSharp => ExportGroupCharacterMapCSharp(mapping),
                ModuleTypes.CodecepticonModules.Powershell => ExportGroupCharacterMapPowerShell(mapping),
                ModuleTypes.CodecepticonModules.Vb6 => ExportGroupCharacterMapVB6(mapping),
                _ => ""
            };
        }

        public static string ExportGroupCharacterMapVB6(Dictionary<string, int> mapping)
        {
            string output = "";
            foreach (KeyValuePair<string, int> map in mapping)
            {
                if (output.Length > 0)
                {
                    output += "\r\n";
                }
                output += CommandLineData.Global.Rewrite.Template.Mapping + $".Add \"{map.Key}\", \"{map.Value}\"";
            }
            return output.Trim();
        }

        public static string ExportGroupCharacterMapCSharp(Dictionary<string, int> mapping)
        {
            string output = "";
            foreach (KeyValuePair<string, int> map in mapping)
            {
                if (output.Length > 0)
                {
                    output += ", ";
                }
                output += $"{{ \"{map.Key}\", {map.Value} }}";
            }
            return output;
        }

        public static string ExportGroupCharacterMapPowerShell(Dictionary<string, int> mapping)
        {
            string output = "";
            foreach (KeyValuePair<string, int> map in mapping)
            {
                if (output.Length > 0)
                {
                    output += "; ";
                }

                output += "$" + CommandLineData.Global.Rewrite.Template.Mapping + $"['{map.Key}'] = {map.Value}";
            }
            return output.Trim();
        }

        public static void SaveExportExternalFileMapping(string file)
        {
            ExternalFileMapping = ExternalFileMapping.Select(v => Convert.ToBase64String(Encoding.UTF8.GetBytes(v))).ToList();

            // Make sure the output path exists.
            string path = Path.GetDirectoryName(file);
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    // Now check again.
                    if (!Directory.Exists(path))
                    {
                        throw new Exception("Could not create path");
                    }
                }
            } catch (Exception e)
            {
                Logger.Error($"Could not create path to save the external file mapping: {path}");
                Logger.Error(e.Message);
                return;
            }

            File.WriteAllLines(file, ExternalFileMapping);
        }
    }
}
