using System.Text.Json;
using AppleSerialization.Json;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.Prototypes
{
    public class ValueInfoWrapper : IPrototype
    {
        public JsonObject Prototype { get; }

        public ValueInfoWrapper()
        {
            Prototype = new JsonObject();
            
            Prototype.Properties.Add(new JsonProperty("$type", "ValueInfo", Prototype, JsonValueKind.String));
            Prototype.Properties.Add(new JsonProperty("valueType", "System.Int32", Prototype, JsonValueKind.String));
            Prototype.Properties.Add(new JsonProperty("value", "0", Prototype, JsonValueKind.String));
        }
    }
}