using AppleSceneEditor.Commands;

namespace AppleSceneEditor.Input.Commands
{
    public class RedoCommand : IKeyCommand
    {
        public bool Disposed { get; private set; }

        private CommandStream _commands;

        public RedoCommand(CommandStream commands)
        {
            (_commands, Disposed) = (commands, false);
        }
        
        public void Execute()
        {
            _commands.RedoCurrentCommand();
        }

        public void Dispose()
        {
            (_commands, Disposed) = (null!, true);
        }
    }
}