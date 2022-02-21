using AppleSerialization.Json;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Commands
{
    public class AddArrayElementCommand : IEditorCommand
    {
        public bool Disposed { get; private set; }

        private JsonArray _array;
        private IMultipleItemsContainer _arrayWidgets;

        private JsonObject _objToAdd;
        private Widget _objWidget;

        public AddArrayElementCommand(JsonArray array, IMultipleItemsContainer arrayWidgets, JsonObject objToAdd,
            Widget objWidget)
        {
            (_array, _arrayWidgets, _objToAdd, _objWidget, Disposed) =
                (array, arrayWidgets, objToAdd, objWidget, false);
        }
        
        public void Execute()
        {
            _array.Add(_objToAdd);
            _arrayWidgets.Widgets.Insert(_arrayWidgets.Widgets.Count - 1, _objWidget);
        }

        public void Undo()
        {
            _array.Remove(_objToAdd);
            _arrayWidgets.Widgets.Remove(_objWidget);
        }

        public void Redo() => Execute();

        public void Dispose()
        {
            (_array, _arrayWidgets, _objToAdd, _objWidget, Disposed) =
                (null!, null!, null!, null!, true);
        }
    }
}