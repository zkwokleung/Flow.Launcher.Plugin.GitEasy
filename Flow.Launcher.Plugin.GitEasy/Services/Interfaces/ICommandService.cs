using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces
{
    public interface ICommandService
    {
        Task<List<Result>> Resolve(Query query);
    }
}
