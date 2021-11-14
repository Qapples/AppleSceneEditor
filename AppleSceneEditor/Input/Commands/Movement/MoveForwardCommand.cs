using AppleSceneEditor.Extensions;
using DefaultEcs;
using GrappleFightNET5.Components.Camera;

namespace AppleSceneEditor.Input.Commands.Movement
{
    public class MoveForwardCommand : IKeyCommand
    {
        public bool Disposed { get; private set; }

        private World _world;
        
        public MoveForwardCommand(World world)
        {
            _world = world;
            Disposed = false;
        }

        public void Execute()
        {
            if (!_world.Has<Camera>()) return;

            ref var camera = ref _world.Get<Camera>();
            ref var properties = ref _world.Get<CameraProperties>();
            var (yawDegrees, pitchDegrees, cameraSpeed) =
                (properties.YawDegrees, properties.PitchDegrees, properties.CameraSpeed);
            
            camera.Position += MovementHelper.GenerateVectorFromDirection(yawDegrees, pitchDegrees,
                MovementHelper.Direction.Forward, (false, false, false), cameraSpeed);
        }

        public void Dispose()
        {
            (_world, Disposed) = (null!, true);
        }
    }
}