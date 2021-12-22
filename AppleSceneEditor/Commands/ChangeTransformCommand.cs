using System.Diagnostics;
using AppleSceneEditor.ComponentFlags;
using DefaultEcs;
using GrappleFightNET5.Components;
using GrappleFightNET5.Components.Collision;
using Microsoft.Xna.Framework;

namespace AppleSceneEditor.Commands
{
    public class ChangeTransformCommand : IEditorCommand
    {
        public bool Disposed { get; private set; }

        private readonly Entity _entity;
        private Vector3 _entityBoxExtent;
        private Transform _oldTransform;
        private Transform _newTransform;

        private readonly bool _scaleComplexBox;

        public ChangeTransformCommand(Entity entity, Transform oldTransform, Transform newTransform,
            bool scaleComplexBox = false)
        {
            (_entity, _oldTransform, _newTransform, Disposed) = (entity, oldTransform, newTransform, false);
            _scaleComplexBox = scaleComplexBox;

            if (entity.Has<ComplexBox>())
            {
                _entityBoxExtent = entity.Get<ComplexBox>().HalfExtent;
            }
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

        private void MoveEntity(ref Transform newTransform)
        {
            _entity.Set(newTransform);

            if (_scaleComplexBox && _entity.Has<ComplexBox>())
            {
               ref var box = ref _entity.Get<ComplexBox>();

               if (newTransform.Matrix.Decompose(out Vector3 newScale, out _, out _))
               {
                   box.HalfExtent = _entityBoxExtent * newScale;
               }
            }

            _entity.World.Set(new EntityTransformChangedFlag(_entity, newTransform));
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}