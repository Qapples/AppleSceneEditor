using AppleSceneEditor.ComponentFlags;
using DefaultEcs;
using GrappleFightNET5.Components.Camera;
using GrappleFightNET5.Components.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Input.Commands
{
    public class SelectEntityCommand : IKeyCommand
    {
        public bool Disposed { get; private set; }

        private World _world;
        private GraphicsDevice _graphicsDevice;

        public SelectEntityCommand(World world, GraphicsDevice graphicsDevice)
        {
            (_world, _graphicsDevice, Disposed) = (world, graphicsDevice, false);
        }

        public void Execute()
        {
            ref var worldCam = ref _world.Get<Camera>();

            MouseState mouseState = Mouse.GetState();
            Viewport viewport = _graphicsDevice.Viewport;
            Vector3 source = new Vector3(mouseState.X, mouseState.Y, 0.5f);

            foreach (Entity entity in _world.GetEntities().With<ComplexBox>().AsEnumerable())
            {
                ref var box = ref entity.Get<ComplexBox>();

                Ray ray = new Ray(worldCam.Position,
                    viewport.Unproject(source, worldCam.ProjectionMatrix, worldCam.ViewMatrix,
                        Matrix.CreateWorld(worldCam.Position, Vector3.Forward, Vector3.Up)));

                float? intercept = box.Intersects(ray);

                if (intercept is not null)
                {
                    //raise a "selectedEntityFlag" by adding a component which let's everyone that has access to our
                    //world know that we have selected an entity.
                    _world.Set(new SelectedEntityFlag(entity));
                }
            }
        }

        public void Dispose()
        {
            (_world, _graphicsDevice, Disposed) = (null!, null!, true);
        }
    }
}