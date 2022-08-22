using DefaultEcs;
using GrappleFight.Components;
using Microsoft.Xna.Framework;

namespace AppleSceneEditor.ComponentFlags
{
    public readonly struct EntityTransformChangedFlag
    {
        //we use a property instead of a field for the current transform as to save a little bit of space.
        
        public readonly Entity ChangedEntity;

        public readonly Matrix CurrentTransform => ChangedEntity.GetWorldMatrix();
        public readonly Matrix PreviousTransform;

        public EntityTransformChangedFlag(Entity changedEntity, Matrix previousTransform) =>
            (ChangedEntity, PreviousTransform) = (changedEntity, previousTransform);
    }
}