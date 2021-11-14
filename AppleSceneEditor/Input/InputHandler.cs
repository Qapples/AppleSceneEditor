using System;
using System.Collections.Generic;
using System.Diagnostics;
using AppleSceneEditor.Input.Commands;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Input
{
    public class InputHandler : IDisposable
    {
        private List<CommandEntry> _commands;
        private IKeyCommand[] _outgoingCommands;

        private static readonly IKeyCommand[] EmptyCommands = Array.Empty<IKeyCommand>();
        
        public bool Disposed { get; private set; }
        public bool CanBeHeld { get; init; }

        /// <summary>
        /// Determines how many commands can be returned at once from GetCommand methods.
        /// </summary>
        public const int MaxCommandsActivated = 5;
#if DEBUG
        private const string MaxCmdName = nameof(MaxCommandsActivated);
#endif
        
        public InputHandler(bool canBeHeld) : this(new List<CommandEntry>(), canBeHeld)
        {
        }

        public InputHandler(List<CommandEntry> commands, bool canBeHeld)
        {
            _commands = commands;
            _outgoingCommands = new IKeyCommand[MaxCommandsActivated];
            
            CanBeHeld = canBeHeld;
            Disposed = false;
        }

        public IKeyCommand[] GetCommands(ref KeyboardState kbState, ref KeyboardState prevKbState)
        {
#if DEBUG
            const string methodName = nameof(InputHandler) + "." + nameof(GetCommands);
#endif
            if (!CanBeHeld && kbState == prevKbState)
            {
                return EmptyCommands;
            }

            int index = 0;
            
            foreach (CommandEntry command in _commands)
            {
                if (AllKeysPressed(ref kbState, command.Keys))
                {
                    if (index == MaxCommandsActivated)
                    {
                        Debug.WriteLine($"{methodName}: index has reached the value of {MaxCmdName} " +
                                        $"({MaxCommandsActivated})! Stopping prematurely.");
                        return _outgoingCommands;
                    }

                    _outgoingCommands[index++] = command.Command;
                }
            }
            
            //if the index is zero then that indicates that no commands have been activated
            if (index == 0) return EmptyCommands;
            
            //clean up the last commands as they might have been set beforehand
            for (; index < MaxCommandsActivated; index++)
            {
                _outgoingCommands[index] = IKeyCommand.EmptyCommand;
            }

            return _outgoingCommands;
        }

        public void Dispose()
        {
            foreach (CommandEntry command in _commands)
            {
                command.Command.Dispose();
            }

            foreach (IKeyCommand command in _outgoingCommands)
            {
                if (!command.Disposed) command.Dispose();
            }

            (_commands, _outgoingCommands, Disposed) = (null!, null!, true);
        }

        //we have this method here as using linq would cause a bunch of closure captures and since GetCommand will be
        //called frequently it could potentially cause a memory leak.
        private bool AllKeysPressed(ref KeyboardState kbState, Keys[] keys)
        {
            foreach (Keys key in keys)
            {
                if (kbState.IsKeyUp(key)) return false;
            }

            return true;
        }
    }
}