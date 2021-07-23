using System.Text.Json.Serialization;
using DefaultEcs;

namespace AppleSceneEditor.Serialization.Info
{
    public class WorldInfo : Serializer<WorldInfo>
    {
        public World OutputWorld { get; set; }
        
        [JsonConstructor]
        public WorldInfo(EntityInfo[] info)
        {
            OutputWorld = new World();
            
            foreach (EntityInfo entityInfo in info)
            {
                Entity entity = OutputWorld.CreateEntity();

                foreach (var component in entityInfo.Components)
                {
                    entity.Set(component);
                }
            }
        }
    }
}