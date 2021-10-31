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
        
        public RemoveComponentCommand(JsonObject obj, Grid uiGrid)
        {
            (_obj, _uiGrid, Disposed) = (obj, uiGrid, false);
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

            if (_uiGrid.Parent is not IMultipleItemsContainer container)
            {
                Debug.WriteLine($"{methodName}: uiGrid's parent is either non-existent or not an instance " +
                                $"IMultipleItemsContainer. Cannot re-add component.");
                return;
            }

            container.AddChild(_uiGrid);
        }

        public void Redo() => Execute();

        public void Dispose()
        {
            (_obj, _uiGrid, Disposed) = (null!, null!, true);
        }
    }
}