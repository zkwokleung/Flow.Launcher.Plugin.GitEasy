using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Commands
{
    public class GitCloneCommandOptions
    {
        public string Options { get; set; }
        public string Repo { get; set; }
        public string DestinationFolder { get; set; }
    }
}
