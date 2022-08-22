using AppleSceneEditor.Commands;
using DefaultEcs;
using GrappleFight.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Systems.Axis
{
    public interface IAxis
    {
        World World { get; }
        GraphicsDevice GraphicsDevice { get; }

        void Draw(Effect effect, VertexBuffer buffer, ref Matrix transform, ref Camera worldCam);

        IEditorCommand? HandleInput(ref MouseState mouseState, ref Camera worldCam, bool isRayFired,
            Entity selectedEntity);
    }
}