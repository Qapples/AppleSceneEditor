using System.Collections.Generic;
using System.Text.Json;
using AppleSerialization.Json;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.Prototypes
{
    public class MeshInfoPrototype : IPrototype
    {
        public JsonObject Prototype { get; }

        public MeshInfoPrototype()
        {
            Prototype = new JsonObject();
            
            Prototype.Properties.Add(new JsonProperty("$type", "MeshInfo", Prototype, JsonValueKind.String));
            Prototype.Properties.Add(new JsonProperty("meshIndex", 0, Prototype, JsonValueKind.Number));
            Prototype.Properties.Add(new JsonProperty("skinIndex", 0, Prototype, JsonValueKind.Number));

            Prototype.Children.Add(new JsonObject("meshPath", Prototype, new List<JsonProperty>
            {
                new("path", "", Prototype, JsonValueKind.String),
                new("isContentPath", false, Prototype, JsonValueKind.False)
            }));
        }
    }
}