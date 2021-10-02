using AppleSerialization.Json;

namespace AppleSceneEditor.Wrappers
{
    /// <summary>
    /// Classes who implement this abstract class have a static <see cref="JsonObject"/> representing a prototype.
    /// </summary>
    public abstract class JsonPrototype
    {
        /// <summary>
        /// A <see cref="JsonObject"/> with default data representing the class. 
        /// </summary>
        public static JsonObject Prototype { get; protected set; }
    }
}