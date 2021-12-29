namespace AppleSceneEditor.ComponentFlags
{
    public readonly struct RemovedEntityFlag
    {
        public readonly string RemovedEntityId;

        public RemovedEntityFlag(string removedEntityId) => RemovedEntityId = removedEntityId;
    }
}