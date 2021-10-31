using System.Diagnostics;
using System.Linq;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Commands
{
    public class RemoveComponentCommand : ICommand
    {
        public bool Disposed { get; private set; }
        
        private JsonObject _obj;
        
        private Grid _uiGrid;
        private IMultipleItemsContainer? _uiGirdParent;
        
        public RemoveComponentCommand(JsonObject obj, Grid uiGrid)
        {
            (_obj, _uiGrid, Disposed) = (obj, uiGrid, false);

            if (_uiGrid.Parent is IMultipleItemsContainer parent)
            {
                _uiGirdParent = parent;
            }
            else
            {
                Debug.WriteLine($"{nameof(RemoveComponentCommand)}: parent of provided uiGrid does not have a " +
                                $"parent that implements {nameof(IMultipleItemsContainer)}! Will not be able to undo.");
            }
        }
        
        public void Execute()
        {
            const string methodName = nameof(RemoveComponentCommand) + "." + nameof(Execute);
            
            JsonArray? componentArray = _obj.Parent?.FindArray("components");
            if (componentArray is null) return;
            
            componentArray.Remove(_obj);

            _uiGrid.RemoveFromParent();
        }

        public void Undo()
        {
            const string methodName = nameof(RemoveComponentCommand) + "." + nameof(Undo);
            
            JsonArray? componentArray = _obj.Parent?.FindArray("components");
            if (componentArray is null) return;
            
            componentArray.Add(_obj);

            _uiGirdParent?.AddChild(_uiGrid);
        }

        public void Redo() => Execute();

        public void Dispose()
        {
            (_obj, _uiGrid, Disposed) = (null!, null!, true);
        }
    }
}