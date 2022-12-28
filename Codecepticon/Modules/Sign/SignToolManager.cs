using Codecepticon.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.Sign
{
    class SignToolManager
    {
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern int SHGetSpecialFolderPath(IntPtr hwndOwner, IntPtr lpszPath, int nFolder, int fCreate);

        private const int CSIDL_PROGRAM_FILES = 0x0026;
        private const int CSIDL_PROGRAM_FILESX86 = 0x002a;

        public string Find()
        {
            List<string> systemPaths = new List<string>
            {
                GetSpecialFolder(CSIDL_PROGRAM_FILES).ToLower(),
                GetSpecialFolder(CSIDL_PROGRAM_FILESX86).ToLower()
            }.Distinct().ToList();  // If Codecepticon is compiled as x86 it will get the same folder twice.

            List<string> foundFiles = SearchInFolders(systemPaths);
            if (foundFiles.Count == 0)
            {
                return "";
            }
            else if (foundFiles.Count == 1)
            {
                return foundFiles.First();
            }

            string path = "";

            Logger.Info("");
            Logger.Info("Multiple instances of signtool.exe were found, please select which one you would like to use:");
            Logger.Info("");
            for (int i = 0; i < foundFiles.Count; i++)
            {
                Logger.Info("\t[" + (i + 1) + "] " + foundFiles[i]);
            }
            Logger.Info("");

            while (true)
            {
                Logger.Info("Please enter the number of your selection: ", false);
                var userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out int fileNumber) && fileNumber > 0 && fileNumber <= foundFiles.Count())
                {
                    path = foundFiles[fileNumber - 1];
                    break;
                }
            }

            return path;
        }

        private string GetSpecialFolder(int folder)
        {
            IntPtr path = Marshal.AllocHGlobal(260 * 2); // Unicode.
            SHGetSpecialFolderPath(IntPtr.Zero, path, folder, 0);
            string result = Marshal.PtrToStringUni(path);
            Marshal.FreeHGlobal(path);
            return result;
        }

        private List<string> SearchInFolders(List<string> paths)
        {
            List<string> files = new List<string>();
            foreach (string path in paths)
            {
                string commandOutput = RunSearch(path);

                string[] lines = commandOutput.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    if (!line.ToLower().StartsWith(path))
                    {
                        continue;
                    }
                    else if (files.Contains(line))
                    {
                        continue;
                    }
                    files.Add(line);
                }
            }
            return files;
        }

        private string RunSearch(string path)
        {
            Logger.Info("Running: cd \"" + path + "\" && dir /s /b signtool.exe");
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c cd \"" + path + "\" && dir /s /b signtool.exe";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            return process.StandardOutput.ReadToEnd();
        }

        public bool SignExecutable(string signToolPath, string executable, string pfxFile, string password, ref string stdOutput, ref string stdError)
        {
            // signtool.exe sign /f C:\data\tmp\self-signed.pfx /p Hello /fd SHA256 /tr http://localhost:8888/ C:\data\tmp\SignCerts\SignCerts\bin\Debug\net6.0\SignCerts2.exe
            string commandLineArguments = $"sign /f \"{pfxFile}\" /p \"{password}\" /fd SHA256 \"{executable}\"";
            Logger.Info($"Running: {signToolPath} {commandLineArguments}");

            Process process = new Process();
            process.StartInfo.FileName = signToolPath;
            process.StartInfo.Arguments = commandLineArguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError= true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            
            stdOutput = process.StandardOutput.ReadToEnd().Trim();
            stdError = process.StandardError.ReadToEnd().Trim();

            return stdError.Length == 0;
        }
    }
}
