using System;
using System.Collections.Generic;

namespace AppleSceneEditor.Commands
{
    //TODO: Add docs
    public class CommandStream : IDisposable
    {
        public bool Disposed { get; private set; }
        
        private List<ICommand> _commands;
        private int _currentIndex;
        
        public CommandStream()
        {
            (_commands, _currentIndex, Disposed) = (new List<ICommand>(), -1, false);
        }

        public void AddCommandAndExecute(ICommand command)
        {
            //if we do a new command when the current index is not at the end of the list then we must remove everything
            //after the current index as those commands are invalid since they're now based on outdated data.
            if (_currentIndex != _commands.Count - 1)
            {
                for (int i = _commands.Count - 1; i > _currentIndex; i--)
                {
                    _commands[i].Dispose();
                    _commands.RemoveAt(i);
                }
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

        public void Dispose()
        {
            foreach (ICommand cmd in _commands) cmd.Dispose();

            (_commands, _currentIndex, Disposed) = (null!, 0, true);
        }
    }
}