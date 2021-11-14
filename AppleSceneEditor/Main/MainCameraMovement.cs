using System.Collections.Generic;
using AppleSceneEditor.Extensions;
using GrappleFightNET5.Components.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Direction = AppleSceneEditor.Extensions.MovementHelper.Direction;

namespace AppleSceneEditor
{
    //methods and variables in regards to the movement of the camera.
    //could make this a system but for convince sake we'll have it here since there should only be one camera.
    
    public partial class MainGame
    {
        private readonly Dictionary<string, Keys> _movementKeys = new()
        {
            {"Move Forward", Keys.W},
            {"Move Backward", Keys.S},
            {"Move Left", Keys.A},
            {"Move Right", Keys.D},
        };
        
        private MouseState _previousMouseState;

        private float _yawDegrees;
        private float _pitchDegrees;
        
        private const float CameraSpeed = 0.5f;

        private void UpdateCamera(MouseState mouseState)
        {
            if (_currentScene is null) return;

            KeyboardState kbState = Keyboard.GetState();
            ref var camera = ref _currentScene.World.Get<Camera>();

            // if (kbState[_movementKeys["Move Forward"]] == KeyState.Down)
            //     camera.Position += GetVelocityVector(Direction.Forward, (false, false, false), CameraSpeed);
            if (kbState[_movementKeys["Move Backward"]] == KeyState.Down)
                camera.Position += GetVelocityVector(Direction.Backwards, (false, false, false), CameraSpeed);
            if (kbState[_movementKeys["Move Left"]] == KeyState.Down)
                camera.Position += GetVelocityVector(Direction.Left, (false, false, false), CameraSpeed);
            if (kbState[_movementKeys["Move Right"]] == KeyState.Down)
                camera.Position += GetVelocityVector(Direction.Right, (false, false, false), CameraSpeed);

            _yawDegrees += (_previousMouseState.X - mouseState.X) / camera.Sensitivity;
            _pitchDegrees += (_previousMouseState.Y - mouseState.Y) / camera.Sensitivity;
            
            camera.RotateFromDegrees(_yawDegrees, _pitchDegrees);
        }

        private Vector3 GetVelocityVector(Direction direction, (bool, bool, bool) axisLock,
            float magnitude) =>
            MovementHelper.GenerateVectorFromDirection(_yawDegrees, _pitchDegrees, direction, axisLock, magnitude);
    }
}