using System;
using System.IO;
using SealFind;

/// <summary>
/// Vorschau-Werkzeug: rendert für jede Welt ein Wimmelbild (einmal normal, einmal mit markierten Robben).
///   dotnet run -- ausgabe/            → alle Welten, Level 2
///   dotnet run -- ausgabe/ 3 5        → Welt 4, Level 6
/// </summary>
static class Program
{
    static void Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : "preview";
        Directory.CreateDirectory(dir);
        if (args.Length >= 3)
        {
            var def = ContentDatabase.BuildLevel(int.Parse(args[1]), int.Parse(args[2]));
            SceneRender.Render(def, Path.Combine(dir, $"w{def.World}_l{def.Level}.png"), false);
            SceneRender.Render(def, Path.Combine(dir, $"w{def.World}_l{def.Level}_loesung.png"), true);
            return;
        }
        for (int w = 0; w < ContentDatabase.Worlds.Count; w++)
        {
            var def = ContentDatabase.BuildLevel(w, 1);
            var name = ContentDatabase.Worlds[w].Id;
            SceneRender.Render(def, Path.Combine(dir, $"{name}.png"), false);
            SceneRender.Render(def, Path.Combine(dir, $"{name}_loesung.png"), true);
        }
    }
}
