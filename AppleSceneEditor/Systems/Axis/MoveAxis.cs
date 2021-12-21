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
                CenterOffset = Vector3.Zero,
                RotationOffset = Quaternion.Identity,
                DrawType = DrawType.Solid,
                HalfExtent = new Vector3(5f, 0.5f, 0.5f),
            };

            _yAxisBox = _xAxisBox;
            _yAxisBox.HalfExtent = new Vector3(0.5f, 5f, 0.5f);

            _zAxisBox = _xAxisBox;
            _zAxisBox.HalfExtent = new Vector3(0.5f, 0.5f, 5f);
        }
        

        public void Draw(Effect effect, VertexBuffer buffer, ref Transform transform, ref Camera worldCam)
        {
            transform.Matrix.Decompose(out _, out Quaternion rotation, out Vector3 position);

            //we can save a few matrix copies by getting the view matrix here.
            //none of the boxes here have any valuable offset so we only need one world matrix
            Matrix world = _xAxisBox.GetWorldMatrix(position, rotation, Vector3.One, true);
            ref Matrix projection = ref worldCam.ProjectionMatrix;
            Matrix view = worldCam.ViewMatrix;

            _xAxisBox.Draw(GraphicsDevice, effect, Color.Red, ref world, ref view, ref projection, null, buffer);
            _yAxisBox.Draw(GraphicsDevice, effect, Color.Green, ref world, ref view, ref projection, null, buffer);
            _zAxisBox.Draw(GraphicsDevice, effect, Color.Blue, ref world, ref view, ref projection, null, buffer);
        }

        public IEditorCommand? HandleInput(ref MouseState mouseState, ref Camera worldCam, bool isRayFired,
            Entity selectedEntity)
        {
            ref var transform = ref selectedEntity.Get<Transform>();

            if (isRayFired && _axisSelectedFlag == 0)
            {
                Viewport viewport = GraphicsDevice.Viewport;

                transform.Matrix.Decompose(out _, out Quaternion rotation, out Vector3 position);
                
                //the axis boxes do NOT have offsets.
                int xHit = worldCam.FireRayHit(ref _xAxisBox, ref position, ref rotation, ref viewport) ? 1 : 0;
                int yHit = worldCam.FireRayHit(ref _yAxisBox, ref position, ref rotation, ref viewport) ? 1 : 0;
                int zHit = worldCam.FireRayHit(ref _zAxisBox, ref position, ref rotation, ref viewport) ? 1 : 0;

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

                Vector3 movementVector = _axisSelectedFlag switch
                {
                    1 => new Vector3(movementValue, 0f, 0f),
                    2 => new Vector3(0f, movementValue, 0f),
                    3 => new Vector3(0f, 0f, movementValue),
                    _ => Vector3.Zero
                };

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