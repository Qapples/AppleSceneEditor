using DefaultEcs;
using GrappleFightNET5.Components;

namespace AppleSceneEditor.ComponentFlags
{
    public readonly struct EntityTransformChangedFlag
    {
        //we use a property instead of a field for the current transform as to save a little bit of space.
        
        public readonly Entity ChangedEntity;

        public readonly Transform CurrentTransform => ChangedEntity.Get<Transform>();
        public readonly Transform PreviousTransform;

        public EntityTransformChangedFlag(Entity changedEntity, Transform previousTransform) =>
            (ChangedEntity, PreviousTransform) = (changedEntity, previousTransform);
    }
}