using DefaultEcs;
using GrappleFightNET5.Components.Transform;

namespace AppleSceneEditor.Commands
{
    public class ChangeTransformCommand : IEditorCommand
    {
        public bool Disposed { get; private set; }

        private readonly Entity _entity;
        private readonly Transform _oldTransform;
        private readonly Transform _newTransform;

        public ChangeTransformCommand(Entity entity, Transform oldTransform, Transform newTransform)
        {
            (_entity, _oldTransform, _newTransform, Disposed) = (entity, oldTransform, newTransform, false);
        }

        public void Execute()
        {
            _entity.Set(_newTransform);
        }

        public void Undo()
        {
            _entity.Set(_oldTransform);
        }

        public void Redo() => Execute();

        public void Dispose()
        {
            Disposed = true;
        }
    }
}