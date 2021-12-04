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
            MoveEntity(ref _newTransform);
        }

        public void Undo()
        {
            MoveEntity(ref _oldTransform);
        }

        public void Redo() => Execute();

        private void MoveEntity(ref Transform transform)
        {
            _entity.Set(transform);

            if (_entity.Has<ComplexBox>())
            {
                _entity.Get<ComplexBox>().Center = transform.Matrix.Translation;
            }
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}