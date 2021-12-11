using AppleSceneEditor.Commands;
using AppleSceneEditor.ComponentFlags;
using DefaultEcs;
using GrappleFightNET5.Components;
using GrappleFightNET5.Components.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Systems.Axis
{
    public class MoveAxis : IAxis
    {
        public World World { get; private set; }
        
        public GraphicsDevice GraphicsDevice { get; private set; }

        private ComplexBox _xAxisBox;
        private ComplexBox _yAxisBox;
        private ComplexBox _zAxisBox;

        private int _axisSelectedFlag;

        private Transform _previousTransform;
        private MouseState _previousMouseState;

        public MoveAxis(World world, GraphicsDevice graphicsDevice)
        {
            (GraphicsDevice, World) = (graphicsDevice, world);
            
            _xAxisBox = new ComplexBox
            {
                Center = Vector3.Zero,
                DrawType = DrawType.Solid,
                HalfExtent = new Vector3(5f, 0.5f, 0.5f),
                Rotation = Quaternion.Identity
            };

            _yAxisBox = _xAxisBox;
            _yAxisBox.HalfExtent = new Vector3(0.5f, 5f, 0.5f);

            _zAxisBox = _xAxisBox;
            _zAxisBox.HalfExtent = new Vector3(0.5f, 0.5f, 5f);
        }

        public void Draw(Effect effect, VertexBuffer buffer, ref Transform transform, ref Camera worldCam)
        {
            _xAxisBox.Center = transform.Matrix.Translation;
            _yAxisBox.Center = transform.Matrix.Translation;
            _zAxisBox.Center = transform.Matrix.Translation;

            _xAxisBox.Draw(GraphicsDevice, effect, Color.Red, ref worldCam, null, buffer);
            _yAxisBox.Draw(GraphicsDevice, effect, Color.Green, ref worldCam, null, buffer);
            _zAxisBox.Draw(GraphicsDevice, effect, Color.Blue, ref worldCam, null, buffer);
        }

        public IEditorCommand? HandleInput(ref MouseState mouseState, ref Camera worldCam, bool isRayFired,
            Entity selectedEntity)
        {
            ref var transform = ref selectedEntity.Get<Transform>();
            
            if (isRayFired && _axisSelectedFlag == 0)
            {
                Viewport viewport = GraphicsDevice.Viewport;
                
                int xHit = worldCam.FireRayHit(_xAxisBox, viewport) ? 1 : 0;
                int yHit = worldCam.FireRayHit(_yAxisBox, viewport) ? 1 : 0;
                int zHit = worldCam.FireRayHit(_zAxisBox, viewport) ? 1 : 0;

                //there must be only one axis hit in order for it to be selected (avoid situations where 
                //more than one axis is hit)
                if (xHit + yHit + zHit < 2)
                {
                    _axisSelectedFlag = (xHit) + (yHit * 2) + (zHit * 3);
                    _previousTransform = transform;
                }
            }
            else if (mouseState.LeftButton == ButtonState.Pressed && _axisSelectedFlag > 0)
            {
                int movementValue = mouseState.Y - _previousMouseState.Y;

                Vector3 movementVector = Vector3.Zero;
                if (_axisSelectedFlag == 1) movementVector.X = movementValue;
                if (_axisSelectedFlag == 2) movementVector.Y = movementValue;
                if (_axisSelectedFlag == 3) movementVector.Z = movementValue;

                transform.Matrix *= Matrix.CreateTranslation(movementVector * 0.25f);
            }
            else if (mouseState.LeftButton == ButtonState.Released && _axisSelectedFlag > 0)
            {
                World.Set(new EntityTransformChangedFlag(selectedEntity, _previousTransform));
                _axisSelectedFlag = 0;

                return new ChangeTransformCommand(selectedEntity, _previousTransform, transform);
            }

            _previousMouseState = mouseState;

            return null;
        }
    }
}