using System.Diagnostics;
using GrappleFightNET5.Scenes;

namespace AppleSceneEditor.Input.Commands
{
    public class SaveCommand : IKeyCommand
    {
        public bool Disposed { get; private set; }
        
        private ComponentPanelHandler _handler;
        private Scene _scene;

        public SaveCommand(ComponentPanelHandler handler, Scene scene)
        {
            (_handler, _scene, Disposed) = (handler, scene, false);
        }

        public void Execute()
        {
            _handler.SaveToScene(_scene);
        }
        
        public void Dispose()
        {
            (_handler, _scene, Disposed) = (null!, null!, true);
        }
    }
}