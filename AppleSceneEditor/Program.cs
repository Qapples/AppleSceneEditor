using System;

namespace AppleSceneEditor
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var game = new MainGame(args))
                game.Run();
        }
    }
}