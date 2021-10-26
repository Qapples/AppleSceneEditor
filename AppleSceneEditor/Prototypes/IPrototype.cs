using AppleSerialization.Json;

namespace AppleSceneEditor.Prototypes
{
    public interface IPrototype
    {
        JsonObject Prototype { get; }
    }
}