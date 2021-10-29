using System.Collections.Generic;

namespace AppleSceneEditor.Commands
{
    public class CommandStream
    {
        private List<ICommand> _commands;
        private int _currentIndex;
        
        public CommandStream()
        {
            _commands = new List<ICommand>();
            _currentIndex = 0;
        }

        public void AddCommandAndExecute(ICommand command)
        {
            //if we do a new command when the current index is not at the end of the list then we must remove everything
            //after the current index as those commands are invalid since they're now based on outdated data.
            if (_currentIndex != _commands.Count - 1)
            {
                _commands.RemoveRange(_currentIndex + 1, _commands.Count - _currentIndex);
            }
            
            _commands.Add(command);
            _currentIndex++;
            command.Execute();
        }

        public void UndoCurrentCommand()
        {
            if (_currentIndex == 0) return;
            
            _commands[_currentIndex--].Undo();
        }

        public void RedoCurrentCommand()
        {
            if (_currentIndex >= _commands.Count - 1) return;
            
            _commands[++_currentIndex].Redo();
        }
    }
}