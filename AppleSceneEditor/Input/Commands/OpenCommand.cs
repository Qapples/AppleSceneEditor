namespace AppleSceneEditor.Input.Commands
{
    public class OpenCommand : IKeyCommand
    {
        public bool Disposed { get; private set; }

        private MainGame _game;

        public OpenCommand(MainGame game)
        {
            (_game, Disposed) = (game, false);
        }
        
        public void Execute()
        {
            _game.MenuFileOpen(null, null);
        }
        
        public void Dispose()
        {
            (_game, Disposed) = (null!, true);
        }
    }
}