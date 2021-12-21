using AppleSceneEditor.ComponentFlags;
using DefaultEcs;
using GrappleFightNET5.Components;
using GrappleFightNET5.Components.Collision;

namespace AppleSceneEditor.Commands
{
    public class ChangeTransformCommand : IEditorCommand
    {
        public bool Disposed { get; private set; }

        private readonly Entity _entity;
        private Transform _oldTransform;
        private Transform _newTransform;

        public ChangeTransformCommand(Entity entity, Transform oldTransform, Transform newTransform)
        {
            (_entity, _oldTransform, _newTransform, Disposed) = (entity, oldTransform, newTransform, false);
        }

        public void Execute()
        {
            MoveEntity(ref _newTransform, true);
        }

        public void Undo()
        {
            MoveEntity(ref _oldTransform, false);
        }

        public void Redo() => Execute();

        private void MoveEntity(ref Transform transform, bool newTransform)
        {
            _entity.Set(transform);

            _entity.World.Set(new EntityTransformChangedFlag(_entity, newTransform ? _oldTransform : _newTransform));
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}