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
            //"OnGotKeyboardFocus" causes IsKeyboardFocused to be false. Weird method naming.
            _mainGrid.OnLostKeyboardFocus();
            GlobalFlag.SetFlag(GlobalFlags.UserControllingSceneViewer, false);
        }

        public void Dispose()
        {
            (_mainGrid, Disposed) = (null!, true);
        }
    }
}