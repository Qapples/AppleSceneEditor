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
    public class ScaleAxis : IAxis
    {
        public World World { get; private set; }

        public GraphicsDevice GraphicsDevice { get; private set; }

        private VertexPositionColor[] _axisLines;

        private ComplexBox _xAxisBox;
        private ComplexBox _yAxisBox;
        private ComplexBox _zAxisBox;

        private int _axisSelectedFlag;

        private Transform _previousTransform;
        private MouseState _previousMouseState;

        public ScaleAxis(World world, GraphicsDevice graphicsDevice)
        {
            (GraphicsDevice, World) = (graphicsDevice, world);

            float offset = 7.5f;

            _xAxisBox = new ComplexBox
            {
                CenterOffset = new Vector3(offset, 0f, 0f),
                RotationOffset = Quaternion.Identity,
                DrawType = DrawType.Solid,
                HalfExtent = new Vector3(1f, 1f, 1f),
            };

            _yAxisBox = _xAxisBox;
            _yAxisBox.CenterOffset = new Vector3(0f, offset, 0f);

            _zAxisBox = _xAxisBox;
            _zAxisBox.CenterOffset = new Vector3(0f, 0f, offset);

            _axisLines = new[]
            {
                //x axis line
                new VertexPositionColor(Vector3.Zero, Color.Red),
                new VertexPositionColor(new Vector3(offset, 0f, 0f), Color.Red),

                //y axis line
                new VertexPositionColor(Vector3.Zero, Color.Green),
                new VertexPositionColor(new Vector3(0f, offset, 0f), Color.Green),

                //z axis line
                new VertexPositionColor(Vector3.Zero, Color.Blue),
                new VertexPositionColor(new Vector3(0f, 0f, offset), Color.Blue),
            };
        }

        public void Draw(Effect effect, VertexBuffer buffer, ref Transform transform, ref Camera worldCam)
        {
            //draw the lines to the boxes
            buffer.SetData(_axisLines);

            transform.Matrix.Decompose(out Vector3 _, out Quaternion rotation, out Vector3 position);
            Matrix posMatrix = Matrix.CreateTranslation(position);

            if (effect is IEffectMatrices matrices)
            {
                matrices.World = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up) *
                                 Matrix.CreateFromQuaternion(rotation) *
                                 posMatrix;

                matrices.View = worldCam.ViewMatrix;
                matrices.Projection = worldCam.ProjectionMatrix;
            }

            GraphicsDevice.SetVertexBuffer(buffer);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, _axisLines.Length);
            }

            //each box has different offsets so we need to make three world matrices
            Matrix xWorld = _xAxisBox.GetWorldMatrix(Vector3.Zero, rotation, Vector3.One, false);
            Matrix yWorld = _yAxisBox.GetWorldMatrix(Vector3.Zero, rotation, Vector3.One, false);
            Matrix zWorld = _zAxisBox.GetWorldMatrix(Vector3.Zero, rotation, Vector3.One, false);
            (xWorld, yWorld, zWorld) = (xWorld * posMatrix, yWorld * posMatrix, zWorld * posMatrix);

            ref Matrix projection = ref worldCam.ProjectionMatrix;
            Matrix view = worldCam.ViewMatrix;

            _xAxisBox.Draw(GraphicsDevice, effect, Color.Red, ref xWorld, ref view, ref projection, null, buffer);
            _yAxisBox.Draw(GraphicsDevice, effect, Color.Green, ref yWorld, ref view, ref projection, null, buffer);
            _zAxisBox.Draw(GraphicsDevice, effect, Color.Blue, ref zWorld, ref view, ref projection, null, buffer);
        }

        public IEditorCommand? HandleInput(ref MouseState mouseState, ref Camera worldCam, bool isRayFired,
            Entity selectedEntity)
        {
            ref var transform = ref selectedEntity.Get<Transform>();

            if (isRayFired && _axisSelectedFlag == 0)
            {
                Viewport viewport = GraphicsDevice.Viewport;

                transform.Matrix.Decompose(out _, out Quaternion rotation, out Vector3 position);

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
                float movementValue = (mouseState.Y - _previousMouseState.Y) * 0.035f;

                Vector3 scaleAxis = _axisSelectedFlag switch
                {
                    1 => Vector3.UnitY,
                    2 => Vector3.UnitX,
                    3 => Vector3.UnitZ,
                    _ => Vector3.Zero
                };

                transform.Matrix *= Matrix.CreateScale(scaleAxis * movementValue);
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