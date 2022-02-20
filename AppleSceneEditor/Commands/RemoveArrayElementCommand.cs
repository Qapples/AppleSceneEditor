using System.Diagnostics;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Commands
{
    public class RemoveArrayElementCommand : IEditorCommand
    {
        public bool Disposed { get; private set; }

        private JsonArray _array;
        private JsonObject _objToRemove;

        private Grid _uiGrid;
        private IMultipleItemsContainer? _uiGridParent;

        private int _objIndex;
        
        public RemoveArrayElementCommand(JsonArray array, JsonObject objToRemove, Grid uiGrid)
        {
            (_array, _objToRemove, _uiGrid, Disposed) = (array, objToRemove, uiGrid, false);
            _objIndex = -1;

            if (_uiGrid.Parent is IMultipleItemsContainer parent)
            {
                _uiGridParent = parent;
            }
            else
            {
                Debug.WriteLine($"{nameof(RemoveComponentCommand)}: parent of provided uiGrid does not have a " +
                                $"parent that implements {nameof(IMultipleItemsContainer)}! Will not be able to undo.");
            }
        }
        
        public void Execute()
        {
#if DEBUG
            const string methodName = nameof(RemoveArrayElementCommand) + "." + nameof(Execute);
#endif
            if (_array.Count < 2)
            {
                Debug.WriteLine($"{methodName}: array length is 1 or less! Cannot remove element. Remove the " +
                                $"array object itself if you desire to remove the array.");
                return;
            }
            
            for (int i = 0; i < _array.Count; i++)
            {
                JsonObject obj = _array[i];

                if (obj == _objToRemove)
                {
                    _array.RemoveAt(i);
                    _objIndex = i;

                    break;
                }
            }
            
            _uiGrid.RemoveFromParent();
        }

        public void Undo()
        {
            if (_objIndex < 0) return;
            
            if (_objIndex < _array.Count)
            {
                _array.Insert(_objIndex, _objToRemove);
                _uiGridParent?.Widgets.Insert(_objIndex, _uiGrid);
            }
            else
            {
                _array.Add(_objToRemove);
                _uiGridParent?.Widgets.Add(_uiGrid);
            }
        }

        public void Redo() => Execute();

        public void Dispose()
        {
            (_array, _objToRemove, _uiGrid, _uiGridParent, Disposed) = (null!, null!, null!, null!, true);
        }
    }
}