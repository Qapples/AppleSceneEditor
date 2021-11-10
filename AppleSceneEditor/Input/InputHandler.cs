using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AppleSceneEditor.Input.Commands;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Input
{
    public class InputHandler : IDisposable
    {
        private Dictionary<KeyboardState, IKeyCommand> _commands;

        public InputHandler()
        {
            _commands = new Dictionary<KeyboardState, IKeyCommand>();
        }

        public InputHandler(Dictionary<KeyboardState, IKeyCommand> commands)
        {
            _commands = commands;
        }

        public IKeyCommand? GetCommand(ref KeyboardState kbState, ref KeyboardState prevKbState) =>
            kbState != prevKbState && _commands.TryGetValue(kbState, out var command) ? command : null;

        public void Dispose()
        {
            foreach (IKeyCommand command in _commands.Values)
            {
                command.Dispose();
            }

            _commands = null!;
        }
    }
}