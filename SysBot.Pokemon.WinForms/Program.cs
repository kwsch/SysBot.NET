using System;
using System.Windows.Forms;
using PKHeX.Core;
using SysBot.Pokemon.Z3;

namespace SysBot.Pokemon.WinForms;

internal static class Program
{
    public static readonly ProgramConfig Config;

    static Program()
    {
        var cmd = Environment.GetCommandLineArgs();
        var use = Array.Find(cmd, z => z.EndsWith(".json"));
        var cfg = Config = ConfigLoader.LoadConfig(use);
        Application.SetCompatibleTextRenderingDefault(false);
        if (cfg.DarkMode)
            Application.SetColorMode(SystemColorMode.Dark);

        PokeTradeBotSWSH.SeedChecker = new Z3SeedSearchHandler<PK8>();
    }

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
#if NETCOREAPP
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
#endif

        Application.EnableVisualStyles();
        Application.Run(new Main());
    }
}
