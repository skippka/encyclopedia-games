namespace GameEncyclopedia;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Database.Prepare();
        Application.Run(new MainForm());
    }
}
