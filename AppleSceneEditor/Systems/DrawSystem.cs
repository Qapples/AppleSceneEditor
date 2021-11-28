using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AppleScene.Helpers;
using AppleScene.Rendering;
using AppleSceneEditor.ComponentFlags;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using GrappleFightNET5.Components.Camera;
using GrappleFightNET5.Components.Collision;
using GrappleFightNET5.Components.Events;
using GrappleFightNET5.Components.Misc;
using GrappleFightNET5.Components.Transform;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Systems
{
    /// <summary>
    /// System responsible for drawing 3d objects.
    /// </summary>
    [With(typeof(Transform))]
    [WithEither(typeof(ComplexBox), typeof(MeshData))]
    public sealed class DrawSystem : AEntitySetSystem<GameTime>
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly BasicEffect _boxEffect;
        private readonly VertexBuffer _boxVertexBuffer;

        private ComplexBox _xAxisBox;
        private ComplexBox _yAxisBox;
        private ComplexBox _zAxisBox;

        //0 means no axis is selected, 1 is x-axis, 2 is y-axis, and 3 is z-axis
        private int _axisSelectedFlag;

        private MouseState _previousMouseState;

        private static readonly RasterizerState
            SolidState = new() {FillMode = FillMode.Solid, CullMode = CullMode.None};

        //there should be 36 vertices in every ComplexBox when drawing them.

        public DrawSystem(World world, GraphicsDevice graphicsDevice) : this(world, new DefaultParallelRunner(1),
            graphicsDevice)
        {
        }

        public DrawSystem(World world, IParallelRunner runner, GraphicsDevice graphicsDevice) : base(world, runner)
        {
            _graphicsDevice = graphicsDevice;
            _boxVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), 36, BufferUsage.WriteOnly);
            _boxEffect = new BasicEffect(_graphicsDevice)
                {Alpha = 1, VertexColorEnabled = true, LightingEnabled = false};

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

        protected override void Update(GameTime gameTime, in Entity entity)
        {
            //get the camera from the world. The camera can be apart of any entity, but there should be only one
            //camera per world.
            ref var worldCam = ref World.Get<Camera>();
            ref var transform = ref entity.Get<Transform>();

            if (entity.Has<MeshData>())
            {
                var meshData = entity.Get<MeshData>();

                //Note: CollectionsMarshal.AsSpan(List<T>) is unsafe and therefore the incoming list must not be edited.

                if (entity.Has<AnimationComponent>())
                {
                    ref var animComponent = ref entity.Get<AnimationComponent>();
                    animComponent.IncrementActives(gameTime.ElapsedGameTime);

                    ReadOnlySpan<ActiveAnimation> inAnimations =
                        CollectionsMarshal.AsSpan(animComponent.ActiveAnimations);
                    meshData.Draw(in transform.Matrix, worldCam.ViewMatrix, in worldCam.ProjectionMatrix,
                        in inAnimations, SolidState);

                    //update events if they have one.
                    if (entity.Has<AnimationEvents>())
                    {
                        ref var events = ref entity.Get<AnimationEvents>();

                        foreach (ActiveAnimation actAnim in animComponent.ActiveAnimations)
                        {
                            ActiveAnimationWrapper wrapper = new() {ActiveAnimation = actAnim};
                            events.Update(ref wrapper, in entity, World);
                        }
                    }

                    animComponent.CleanActives();
                }
                else
                {
                    meshData.Draw(in transform.Matrix, worldCam.ViewMatrix, in worldCam.ProjectionMatrix,
                        ReadOnlySpan<ActiveAnimation>.Empty, SolidState);
                }
            }

            if (entity.Has<ComplexBox>())
            {
                ref var box = ref entity.Get<ComplexBox>();
                box.Draw(_graphicsDevice, _boxEffect, Color.Red, ref worldCam, null, _boxVertexBuffer);

                bool fireRayFlag = GlobalFlag.IsFlagRaised(GlobalFlags.FireEntitySelectionRay);
                
                if (fireRayFlag)
                {
                    //Handle user selection. (i.e. when the user attempts to select an entity in the scene viewer
                    float? intercept = FireRay(ref worldCam, ref box);

                    if (intercept is not null)
                    {
                        //raise a "selectedEntityFlag" by adding a component which let's everyone that has access to our
                        //world know that we have selected an entity.
                        World.Set(new SelectedEntityFlag(entity));
                        GlobalFlag.SetFlag(GlobalFlags.EntitySelected, true);
                    }

                    GlobalFlag.SetFlag(GlobalFlags.FireEntitySelectionRay, false);
                }

                //handle the x, y, and z complex boxes which can be used by the user within the scene editor to
                //manipulate the transform of entities.
                //lots of repeating code here...
                if (World.Has<SelectedEntityFlag>())
                {
                    Entity selectedEntity = World.Get<SelectedEntityFlag>().SelectedEntity;
                    MouseState mouseState = Mouse.GetState();

                    if (selectedEntity.Has<Transform>() && selectedEntity == entity)
                    {
                        ref var selectedTransform = ref selectedEntity.Get<Transform>();

                        _xAxisBox.Center = selectedTransform.Matrix.Translation;
                        _yAxisBox.Center = selectedTransform.Matrix.Translation;
                        _zAxisBox.Center = selectedTransform.Matrix.Translation;

                        _xAxisBox.Draw(_graphicsDevice, _boxEffect, Color.Red, ref worldCam, null, _boxVertexBuffer);
                        _yAxisBox.Draw(_graphicsDevice, _boxEffect, Color.Green, ref worldCam, null, _boxVertexBuffer);
                        _zAxisBox.Draw(_graphicsDevice, _boxEffect, Color.Blue, ref worldCam, null, _boxVertexBuffer);

                        if (fireRayFlag && _axisSelectedFlag == 0)
                        {
                            int xHit = FireRayHit(ref worldCam, ref _xAxisBox) ? 1 : 0;
                            int yHit = FireRayHit(ref worldCam, ref _yAxisBox) ? 1 : 0;
                            int zHit = FireRayHit(ref worldCam, ref _zAxisBox) ? 1 : 0;
                            
                            //there must be only one axis hit in order for it to be selected (avoid situations where 
                            //more than one axis is hit)
                            if (xHit + yHit + zHit < 2)
                            {
                                _axisSelectedFlag = (xHit) + (yHit * 1) + (zHit * 2);
                                Debug.WriteLine($"Hit on axis: {_axisSelectedFlag}");
                            }
                        }
                        else if (mouseState.LeftButton == ButtonState.Pressed && _axisSelectedFlag > 0)
                        {
                            int movementValue = mouseState.Y - _previousMouseState.Y;

                            Vector3 movementVector = Vector3.Zero;
                            if (_axisSelectedFlag == 1) movementVector.X = movementValue;
                            if (_axisSelectedFlag == 2) movementVector.Y = movementValue;
                            if (_axisSelectedFlag == 3) movementVector.Z = movementValue;

                            selectedTransform.Matrix += Matrix.CreateTranslation(movementVector);
                            Debug.WriteLine("Move matrix: " + movementVector);
                        }
                        else if (mouseState.LeftButton == ButtonState.Released && _axisSelectedFlag > 0)
                        {
                            _axisSelectedFlag = 0;
                            Debug.WriteLine("Released");
                        }
                    }

                    _previousMouseState = mouseState;
                }
            }
        }

        private float? FireRay(ref Camera camera, ref ComplexBox box)
        {
            MouseState mouseState = Mouse.GetState();
            Viewport viewport = _graphicsDevice.Viewport;
            Vector3 source = new(mouseState.X, mouseState.Y, 0.5f);
            
            Ray ray = new(camera.Position,
                viewport.Unproject(source, camera.ProjectionMatrix, camera.ViewMatrix,
                    Matrix.CreateWorld(camera.Position, Vector3.Forward, Vector3.Up)));

            return box.Intersects(ray);
        }

        private bool FireRayHit(ref Camera camera, ref ComplexBox box) => FireRay(ref camera, ref box) is not null;

        public override void Dispose()
        {
            _boxVertexBuffer.Dispose();
            _boxEffect.Dispose();
        }
    }
}