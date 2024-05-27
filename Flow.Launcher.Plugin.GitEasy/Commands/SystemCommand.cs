using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Commands
{
    public class SystemCommand
    {

        public static void OpenExplorer(string path = "", Action OnCompleted = null)
        {
            ProcessStartInfo info = new()
            {
                FileName = "explorer.exe",
                Arguments = Path.GetFullPath(path)
            };

            Process.Start(info).WaitForExit();

            OnCompleted?.Invoke();
        }

        public static void OpenVsCode(string path = "", Action OnCompleted = null)
        {
            ProcessStartInfo info = new()
            {
                FileName = "cmd.exe",
                Arguments = $"/c code \"{path}\""
            };

            Process.Start(info).WaitForExit();

            OnCompleted?.Invoke();
        }


    }
}
