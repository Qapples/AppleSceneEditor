using AppleSceneEditor.Commands;

namespace AppleSceneEditor.Input.Commands
{
    public class UndoCommand : IKeyCommand
    {
        public bool Disposed { get; private set; }

        private CommandStream _commands;

        public UndoCommand(CommandStream commands)
        {
            (_commands, Disposed) = (commands, false);
        }

        public void Execute()
        {
            _commands.UndoCurrentCommand();
        }

        public void Dispose()
        {
            (_commands, Disposed) = (null!, true);
        }
    }
}