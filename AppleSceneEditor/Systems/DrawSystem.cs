using System;
using System.Runtime.InteropServices;
using AppleScene.Helpers;
using AppleScene.Rendering;
using AppleSceneEditor.Commands;
using AppleSceneEditor.ComponentFlags;
using AppleSceneEditor.Systems.Axis;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using GrappleFightNET5.Components;
using GrappleFightNET5.Components.Collision;
using GrappleFightNET5.Components.Events;
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
        private GraphicsDevice _graphicsDevice;
        private BasicEffect _boxEffect;
        private VertexBuffer _boxVertexBuffer;
        private CommandStream _commands;

        private MoveAxis _moveAxis;

        private MouseState _previousMouseState;
        private Transform _previousTransform;

        private static readonly RasterizerState
            SolidState = new() {FillMode = FillMode.Solid, CullMode = CullMode.None};

        //there should be 36 vertices in every ComplexBox when drawing them.

        public DrawSystem(World world, GraphicsDevice graphicsDevice, CommandStream commandStream) : this(world,
            new DefaultParallelRunner(1), graphicsDevice, commandStream)
        {
        }

        public DrawSystem(World world, IParallelRunner runner, GraphicsDevice graphicsDevice,
            CommandStream commandStream) : base(world, runner)
        {
            _graphicsDevice = graphicsDevice;
            
            _boxVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), 36, BufferUsage.WriteOnly);
            _boxEffect = new BasicEffect(_graphicsDevice)
                {Alpha = 1, VertexColorEnabled = true, LightingEnabled = false};
            
            _commands = commandStream;

            _moveAxis = new MoveAxis(world, graphicsDevice);
        }

        protected override void Update(GameTime gameTime, in Entity entity)
        {
            //get the camera from the world. The camera can be apart of any entity, but there should be only one
            //camera per world.
            ref var worldCam = ref World.Get<Camera>();
            ref var axisType = ref World.Get<AxisType>();
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
                    float? intercept = worldCam.FireRay(box, _graphicsDevice.Viewport);

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

                        IEditorCommand? axisHandleCommand = null;

                        switch (axisType)
                        {
                            case AxisType.Move:
                                _moveAxis.Draw(_boxEffect, _boxVertexBuffer, ref selectedTransform, ref worldCam);
                                axisHandleCommand = _moveAxis.HandleInput(ref mouseState, ref worldCam, fireRayFlag,
                                    selectedEntity);
                                break;
                            case AxisType.Rotation:
                            case AxisType.Scale:
                            default:
                                break;
                        }

                        if (axisHandleCommand is not null)
                        {
                            _commands.AddCommandAndExecute(axisHandleCommand);
                        }
                    }

                    _previousMouseState = mouseState;
                }
            }
        }

        public override void Dispose()
        {
            _boxVertexBuffer.Dispose();
            _boxEffect.Dispose();

            (_boxVertexBuffer, _boxEffect, _graphicsDevice, _commands) = (null!, null!, null!, null!);
        }
    }
}