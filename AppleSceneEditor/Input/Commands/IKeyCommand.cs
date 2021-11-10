using System;

namespace AppleSceneEditor.Input.Commands
{
    public interface IKeyCommand : IDisposable
    {
        bool Disposed { get; }
        
        void Execute();

        private static readonly IKeyCommand _emptyCommand = new EmptyCommand();
        public static IKeyCommand EmptyCommand => _emptyCommand;
    }
}