using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Input.Commands
{
    public class ControlSceneCameraCommand : IKeyCommand
    {
        public bool Disposed { get; private set; }

        private Grid _mainGrid;
        
        public ControlSceneCameraCommand(Grid mainGird)
        {
            (_mainGrid, Disposed) = (mainGird, false);
        }

        public void Execute()
        {
            //"OnGotKeyboardFocus" causes IsKeyboardFocused to be true. Weird method naming.
            _mainGrid.OnGotKeyboardFocus();
            GlobalFlag.SetFlag(GlobalFlags.UserControllingSceneViewer, true);
        }

        public void Dispose()
        {
            (_mainGrid, Disposed) = (null!, true);
        }
    }
}