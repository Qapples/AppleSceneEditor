using System;
using System.Runtime.InteropServices;
using AppleScene.Helpers;
using AppleScene.Rendering;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using GrappleFightNET5.Components.Camera;
using GrappleFightNET5.Components.Collision.ComplexBox;
using GrappleFightNET5.Components.Events;
using GrappleFightNET5.Components.Misc;
using GrappleFightNET5.Components.Transform;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AppleSceneEditor.Systems
{
    /// <summary>
    /// System responsible for drawing 3d objects.
    /// </summary>
    //TODO: Use a different model class in the future.
    [With(typeof(Transform))]
    [WithEither(typeof(Model), typeof(ComplexBox), typeof(MeshData))]
    public sealed class DrawSystem : AEntitySetSystem<GameTime>
    {
        private readonly GraphicsDevice _graphicsDevice;
        
        public DrawSystem(World world, GraphicsDevice graphicsDevice) : base(world)
        {
            _graphicsDevice = graphicsDevice;
        }

        public DrawSystem(World world, IParallelRunner runner, GraphicsDevice graphicsDevice) : base(world, runner)
        {
            _graphicsDevice = graphicsDevice;
        }

        protected override void Update(GameTime gameTime, in Entity entity)
        {
            //get the camera from the world. The camera can be apart of any entity, but there should be only one
            //camera per world.
            ref var worldCam = ref World.Get<Camera>();
            ref var transform = ref entity.Get<Transform>();

            //model drawing
            if (entity.Has<Model>())
            {
                // all of the bone's are being manipulated accordingly, but Monogame isn't animating it correctly due to something being
                // messed up in the .dae file being tested or Monogame's model implementation. The Model class in Monogame
                // has a Meshes property, and each mesh in that property has a parent bone. Each Mesh has a parent bone
                // property, but it only takes in account THAT bone and not any of it's descendants. Therefore, at least with
                // the current model.dae file im testing, only one bone animation can be applied to the entire mesh.
                var model = entity.Get<Model>();

                model.Draw(transform.Matrix, worldCam.ViewMatrix, worldCam.ProjectionMatrix);
            }

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
                        in inAnimations, ComplexBoxHelper.SolidState);

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
                        ReadOnlySpan<ActiveAnimation>.Empty, ComplexBoxHelper.SolidState);
                }
            }

            if (entity.Has<ComplexBox>())
            {
                ref var box = ref entity.Get<ComplexBox>();
                box.Draw(Color.Red, in worldCam, _graphicsDevice);
            }
        }
    }
}