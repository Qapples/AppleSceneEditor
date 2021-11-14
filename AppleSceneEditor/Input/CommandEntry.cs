using AppleSceneEditor.Input.Commands;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Input
{
    public readonly struct CommandEntry
    {
        public readonly Keys[] Keys;
        public readonly IKeyCommand Command;

        public CommandEntry(Keys[] keys, IKeyCommand command) => (Keys, Command) = (keys, command);
    }
}