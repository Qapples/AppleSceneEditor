using DefaultEcs;
using GrappleFight.Components;
using GrappleFight.Runtime;
using Microsoft.Xna.Framework;

namespace AppleSceneEditor.Input.Commands
{
    public class ReloadCommand : IKeyCommand
    {
        public bool Disposed { get; private set; }

        private readonly MainGame _game;
        private readonly World _currentSceneWorld;
        private readonly string? _currentSceneDirectory;

        public ReloadCommand(MainGame game, World currentSceneWorld, string? currentSceneDirectory)
        {
            _game = game;
            _currentSceneWorld = currentSceneWorld;
            _currentSceneDirectory = currentSceneDirectory;
        }
        
        public void Execute()
        {
            if (_currentSceneDirectory is null) return;

            Camera prevCamera = _currentSceneWorld.Get<Camera>();
            CameraProperties prevProperties = _currentSceneWorld.Get<CameraProperties>();
            
            Scene scene = _game.InitScene(_currentSceneDirectory);
            
            scene.World.Set(prevCamera);
            scene.World.Set(prevProperties);
            
            _currentSceneWorld.Dispose();
        }
        
        public void Dispose()
        {
            Disposed = true;
        }
    }
}