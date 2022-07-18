using System;
using System.Diagnostics;
using AppleSceneEditor.Commands;
using AppleSceneEditor.ComponentFlags;
using DefaultEcs;
using GrappleFightNET5.Collision;
using GrappleFightNET5.Collision.Hulls;
using GrappleFightNET5.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Systems.Axis
{
    //TODO: Make rotate axis use circle axis instead of square axis!!!
    
    public class RotateAxis : IAxis
    {
        public World World { get; private set; }
        
        public GraphicsDevice GraphicsDevice { get; private set; }

        private ComplexBox _xAxisBox;
        private ComplexBox _yAxisBox;
        private ComplexBox _zAxisBox;

        private int _axisSelectedFlag;

        private Matrix _previousTransform;
        private MouseState _previousMouseState;

        public RotateAxis(World world, GraphicsDevice graphicsDevice)
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

        public void Draw(Effect effect, VertexBuffer buffer, ref Matrix transform, ref Camera worldCam)
        {
            transform.Decompose(out _, out Quaternion rotation, out Vector3 position);

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
            Matrix entityWorldMatrix = selectedEntity.GetWorldMatrix();

            if (isRayFired && _axisSelectedFlag == 0)
            {
                Viewport viewport = GraphicsDevice.Viewport;

                entityWorldMatrix.Decompose(out _, out Quaternion rotation, out Vector3 position);

                int xHit = worldCam.FireRayHit(ref _xAxisBox, ref position, ref rotation, ref viewport) ? 1 : 0;
                int yHit = worldCam.FireRayHit(ref _yAxisBox, ref position, ref rotation, ref viewport) ? 1 : 0;
                int zHit = worldCam.FireRayHit(ref _zAxisBox, ref position, ref rotation, ref viewport) ? 1 : 0;

                //there must be only one axis hit in order for it to be selected (avoid situations where 
                //more than one axis is hit)
                if (xHit + yHit + zHit == 1)
                {
                    _axisSelectedFlag = (xHit) + (yHit * 2) + (zHit * 3);
                    _previousTransform = entityWorldMatrix;
                }
            }
            else if (mouseState.LeftButton == ButtonState.Pressed && _axisSelectedFlag > 0)
            {
                float movementValue = (mouseState.Y - _previousMouseState.Y) * 0.035f;
                
                entityWorldMatrix.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 position);

                Vector3 rotateAxis = _axisSelectedFlag switch
                {
                    1 => Vector3.Up, //yaw,
                    2 => Vector3.Right, //pitch
                    3 => Vector3.Forward, //roll
                    _ => Vector3.Zero
                };

                //reconstruct the matrix, rotation should be in place rather than around the world
                selectedEntity.SetWorldMatrix(Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up) *
                                              Matrix.CreateScale(scale) *
                                              Matrix.CreateFromQuaternion(rotation) *
                                              Matrix.CreateFromAxisAngle(rotateAxis, movementValue) *
                                              Matrix.CreateTranslation(position));
            }
            else if (mouseState.LeftButton == ButtonState.Released && _axisSelectedFlag > 0)
            {
                _axisSelectedFlag = 0;

                return new ChangeTransformCommand(selectedEntity, _previousTransform, entityWorldMatrix);
            }

            _previousMouseState = mouseState;

            return null;
        }
    }
}