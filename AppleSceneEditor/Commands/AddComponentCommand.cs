using System.Collections.Generic;
using System.Diagnostics;
using AppleSerialization.Json;

namespace AppleSceneEditor.Commands
{
    public class AddComponentCommand : ICommand
    {
        public bool Disposed { get; private set; }

        private JsonObject _prototype;
        private ComponentPanelHandler _handler;
        
        public AddComponentCommand(JsonObject prototype, ComponentPanelHandler handler)
        {
            (_prototype, _handler, Disposed) = (prototype, handler, false);
        }

        private JsonObject? _clone;
        
        public void Execute()
        { 
            _clone = (JsonObject) _prototype.Clone();
            _clone.Parent = _handler.Components.Parent;
            
            _handler.Components.Add(_clone);
            _handler.RebuildUI();
        }

        public void Undo()
        {
#if DEBUG
            const string methodName = nameof(AddComponentCommand) + "." + nameof(Undo);
#endif
            if (_clone is null)
            {
                Debug.WriteLine($"{methodName}: clone is null! Was {nameof(Execute)} called before hand?");
                return;
            }
            
            _handler.Components.Remove(_clone);
            _clone.Parent = null;
            _handler.RebuildUI();
        }

        public void Redo() => Execute();

        public void Dispose()
        {
            (_prototype, _handler, Disposed) = (null!, null!, true);
        }
    }
}