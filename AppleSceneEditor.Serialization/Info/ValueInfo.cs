using System.Text.Json.Serialization;

namespace AppleSceneEditor.Serialization.Info
{
    public readonly struct ValueInfo
    {
        public readonly string ValueType;
        public readonly string Value;

        [JsonConstructor]
        public ValueInfo(string valueType, string value) => (ValueType, Value) = (valueType, value);
    }
}