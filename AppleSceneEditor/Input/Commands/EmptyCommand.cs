namespace AppleSceneEditor.Input.Commands
{
    public class EmptyCommand : IKeyCommand
    {
        public bool Disposed { get; private set; }

        public EmptyCommand()
        {
            Disposed = false;
        }
        
        public void Execute()
        {
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}