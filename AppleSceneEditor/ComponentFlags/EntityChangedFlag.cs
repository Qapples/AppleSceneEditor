using DefaultEcs;

namespace AppleSceneEditor.ComponentFlags
{
    public readonly struct EntityChangedFlag
    {
        public readonly Entity ChangedEntity;

        public EntityChangedFlag(Entity changedEntity) => ChangedEntity = changedEntity;
    }
}