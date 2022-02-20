using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Commands
{
    public class RemoveComponentCommand : IEditorCommand
    {
        public bool Disposed { get; private set; }
        
        private JsonObject _obj;
        
        private Grid _uiGrid;
        private IMultipleItemsContainer? _uiGridParent;

        public RemoveComponentCommand(JsonObject obj, Grid uiGrid)
        {
            (_obj, _uiGrid, Disposed) = (obj, uiGrid, false);

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

        private int _arrIndex;
        
        public void Execute()
        {
#if DEBUG
            const string methodName = nameof(RemoveComponentCommand) + "." + nameof(Execute);
#endif
            JsonArray? componentArray = _obj.Parent?.FindArray("components");
            if (componentArray is null) return;

            //_arrIndex is used to remember the position it was in when we remove it in the case that we undo.
            for (int i = componentArray.Count - 1; i > -1; i--)
            {
                if (componentArray[i] == _obj)
                {
                    _arrIndex = i;
                    componentArray.RemoveAt(i);
                }
            }
            
            _uiGrid.RemoveFromParent();
        }

        public void Undo()
        {
#if DEBUG
            const string methodName = nameof(RemoveComponentCommand) + "." + nameof(Undo);
#endif
            JsonArray? componentArray = _obj.Parent?.FindArray("components");
            if (componentArray is null) return;

            if (_arrIndex < componentArray.Count)
            {
                componentArray.Insert(_arrIndex, _obj);
            }
            else
            {
                componentArray.Add(_obj);
            }

            //we add one to account for the holder widget
            _uiGridParent?.Widgets.Insert(_arrIndex + 1, _uiGrid);
        }

        public void Redo() => Execute();

        public void Dispose()
        {
            (_obj, _uiGrid, Disposed) = (null!, null!, true);
        }
    }
}