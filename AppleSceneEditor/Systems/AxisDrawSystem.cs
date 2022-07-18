using AppleSceneEditor.Commands;
using AppleSceneEditor.ComponentFlags;
using AppleSceneEditor.Systems.Axis;
using DefaultEcs;
using DefaultEcs.System;
using GrappleFightNET5.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Systems
{
    /// <summary>
    /// System responsible for drawing the in-editor transformation axis.
    /// </summary>
    public class AxisDrawSystem : ISystem<GameTime>
    {
        public bool IsEnabled { get; set; }

        private World _world;
        private GraphicsDevice _graphicsDevice;
        private CommandStream _commands;

        private BasicEffect _axisEffect;
        private VertexBuffer _axisVertexBuffer;
        
        private MoveAxis _moveAxis;
        private RotateAxis _rotateAxis;
        private ScaleAxis _scaleAxis;

        public AxisDrawSystem(World world, GraphicsDevice graphicsDevice, CommandStream commands)
        {
            (_world, _graphicsDevice, _commands) = (world, graphicsDevice, commands);
            
            _axisEffect = new BasicEffect(_graphicsDevice)
                {Alpha = 1, VertexColorEnabled = true, LightingEnabled = false};
            _axisVertexBuffer =
                new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), 48, BufferUsage.WriteOnly);
            
            _moveAxis = new MoveAxis(world, graphicsDevice);
            _rotateAxis = new RotateAxis(world, graphicsDevice);
            _scaleAxis = new ScaleAxis(world, graphicsDevice);
        }

        public void Update(GameTime gameTime)
        {
            if (!_world.Has<SelectedEntityFlag>()) return;
            
            ref var worldCam = ref _world.Get<Camera>();
            ref var axisType = ref _world.Get<AxisType>();
            
            Entity selectedEntity = _world.Get<SelectedEntityFlag>().SelectedEntity;
            MouseState mouseState = Mouse.GetState();
            
            bool fireRayFlag = GlobalFlag.IsFlagRaised(GlobalFlags.FireSceneEditorRay);

            if (selectedEntity.Has<Transform>())
            {
                Matrix selectedTransform = selectedEntity.GetWorldMatrix();

                IAxis? currentAxis = axisType switch
                {
                    AxisType.Move => _moveAxis,
                    AxisType.Rotation => _rotateAxis,
                    AxisType.Scale => _scaleAxis,
                    _ => null
                };

                currentAxis?.Draw(_axisEffect, _axisVertexBuffer, ref selectedTransform, ref worldCam);
                IEditorCommand? axisHandleCommand = currentAxis?.HandleInput(ref mouseState, ref worldCam,
                    fireRayFlag, selectedEntity);
                            
                if (axisHandleCommand is not null)
                {
                    _commands.AddCommandAndExecute(axisHandleCommand);
                }
            }
        }

        public void Dispose()
        {
            _axisEffect.Dispose();
            _axisVertexBuffer.Dispose();

            (_world, _graphicsDevice, _commands, _moveAxis, _rotateAxis, _scaleAxis, _axisEffect, _axisVertexBuffer) =
                (null!, null!, null!, null!, null!, null!, null!, null!);
        }
    }
}