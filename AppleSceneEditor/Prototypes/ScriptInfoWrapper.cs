using System.Text.Json;
using AppleSerialization.Json;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.Prototypes
{
    public class ScriptInfoWrapper : IPrototype
    {
        public JsonObject Prototype { get; }

        public ScriptInfoWrapper()
        {
            Prototype = new JsonObject();
            
            Prototype.Properties.Add(new JsonProperty("$type", "ScriptInfo", Prototype, JsonValueKind.String));
            Prototype.Properties.Add(new JsonProperty("name", "", Prototype, JsonValueKind.String));
        }
    }
}