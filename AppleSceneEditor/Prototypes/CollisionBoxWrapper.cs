using System.Text.Json;
using AppleSerialization.Json;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.Prototypes
{
    public class CollisionBoxWrapper : IPrototype
    {
        public JsonObject Prototype { get; }

        public CollisionBoxWrapper()
        {
            Prototype = new JsonObject();
            
            Prototype.Properties.Add(new JsonProperty("$type", "CollisionBoxInfo", Prototype, JsonValueKind.String));
            Prototype.Properties.Add(new JsonProperty("position", "0 0 0", Prototype, JsonValueKind.String));
            Prototype.Properties.Add(new JsonProperty("halfExtent", "0 0 0", Prototype, JsonValueKind.String));
            Prototype.Properties.Add(new JsonProperty("rotation", "0 0 0", Prototype, JsonValueKind.String));
        }
    }
}