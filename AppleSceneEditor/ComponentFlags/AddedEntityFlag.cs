using AppleSerialization.Json;
using DefaultEcs;

namespace AppleSceneEditor.ComponentFlags
{
    public readonly struct AddedEntityFlag
    {
        public readonly Entity AddedEntity;
        public readonly JsonObject AddedJsonObject;

        public AddedEntityFlag(in Entity addedEntity, JsonObject addedJsonObject) =>
            (AddedEntity, AddedJsonObject) = (addedEntity, addedJsonObject);
    }
}