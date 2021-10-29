using System;

namespace AppleSceneEditor.Commands
{
    //TODO: Add docs
    public interface ICommand : IDisposable
    {
        bool Disposed { get; }
        
        void Execute();
        void Undo();
        void Redo();
    }
}