using Flow.Launcher.Plugin.GitEasy.Utilities;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;

public interface ICommand
{
    string Key { get; }
    string Title { get; }
    string Description { get; }
    string IconPath { get => Icons.Logo; }
    List<Result> Resolve(string query, string actionKeyword);
}
