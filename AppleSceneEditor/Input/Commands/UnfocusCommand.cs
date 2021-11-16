using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Input.Commands
{
    public class UnfocusCommand : IKeyCommand
    {
        public bool Disposed { get; private set; }

        private Grid _mainGrid;
        
        public UnfocusCommand(Grid mainGrid)
        {
            (_mainGrid, Disposed) = (mainGrid, false);
        }

        public void Execute()
        {
            _mainGrid.OnLostKeyboardFocus();
        }

        public void Dispose()
        {
            (_mainGrid, Disposed) = (null!, true);
        }
    }
}