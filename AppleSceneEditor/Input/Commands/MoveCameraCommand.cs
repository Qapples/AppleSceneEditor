using AppleSceneEditor.Extensions;
using DefaultEcs;
using GrappleFightNET5.Components;
using Microsoft.Xna.Framework;
using Direction = AppleSceneEditor.Extensions.MovementHelper.Direction;

namespace AppleSceneEditor.Input.Commands
{
    public class MoveCameraCommand : IKeyCommand
    {
        public bool Disposed { get; private set; }
        public Direction Direction { get; }

        private World _world;

        public MoveCameraCommand(Direction direction, World world)
        {
            (Direction, _world, Disposed) = (direction, world, false);
        }

        public void Execute()
        {
            if (!_world.Has<Camera>() && !_world.Has<CameraProperties>()) return;

            ref var camera = ref _world.Get<Camera>();
            ref var properties = ref _world.Get<CameraProperties>();
            var (yawDegrees, pitchDegrees, cameraSpeed) =
                (properties.YawDegrees, properties.PitchDegrees, properties.CameraSpeed);

            camera.Position += Direction is Direction.Up or Direction.Down
                ? Direction == Direction.Up ? Vector3.Up * cameraSpeed : Vector3.Down * cameraSpeed
                : MovementHelper.GenerateVectorFromDirection(yawDegrees, pitchDegrees, Direction,
                    (false, false, false), cameraSpeed);
        }

        public void Dispose()
        {
            (_world, Disposed) = (null!, true);
        }
    }
}