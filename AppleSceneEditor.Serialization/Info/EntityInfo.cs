using System.Text.Json.Serialization;

namespace AppleSceneEditor.Serialization.Info
{
    public class EntityInfo : Serializer<EntityInfo>
    {
        public string Id { get; set; }
        
        public dynamic[] Components { get; set; }
        
        [JsonConstructor]
        public EntityInfo(string id, dynamic[] components)
        {
            (Id, Components) = (id, components);
        }
    }
}