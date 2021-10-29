using System.Collections.Generic;
using System.Text.Json;
using AppleSerialization.Json;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.Prototypes
{
    public class TextureInfoWrapper : IPrototype
    {
        public JsonObject Prototype { get; }

        public TextureInfoWrapper()
        {
            Prototype = new JsonObject();
            
            Prototype.Properties.Add(new JsonProperty("$type", "TextureInfo", Prototype, JsonValueKind.String));

            Prototype.Children.Add(new JsonObject("texturePath", Prototype, new List<JsonProperty>
            {
                new("path", "", Prototype, JsonValueKind.String),
                new("isContentPath", false, Prototype, JsonValueKind.False)
            }));
        }
    }
}