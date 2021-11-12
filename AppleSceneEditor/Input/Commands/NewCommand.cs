namespace AppleSceneEditor.Input.Commands
{
    public class NewCommand : IKeyCommand
    {
        public bool Disposed { get; private set; }

        private MainGame _game;
        
        public NewCommand(MainGame game)
        {
            (_game, Disposed) = (game, false);
        }

        public void Execute()
        {
            _game.MenuFileNew(null, null);
        }

        public void Dispose()
        {
            (_game, Disposed) = (null!, true);
        }
    }
}