using System.Reflection;
using System.Text.Json.Serialization;
using DefaultEcs;

namespace AppleSceneEditor.Serialization.Info
{
    /// <summary>
    /// Describes the data needed for creating entities.
    /// </summary>
    public class EntityInfo : Serializer<EntityInfo>
    {
        /// <summary>
        /// The ID of an <see cref="Entity"/> when it's created through <see cref="CreateEntity"/>.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// The components of an <see cref="Entity"/> when it's created through <see cref="CreateEntity"/>.
        /// </summary>
        public object[] Components { get; set; }
        
        /// <summary>
        /// Constructs an <see cref="EntityInfo"/> instance (intended to be used in a Json context).
        /// </summary>
        /// <param name="id">The ID of an <see cref="Entity"/> when it's created through <see cref="CreateEntity"/>.
        /// </param>
        /// <param name="components">The components of an <see cref="Entity"/> when it's created through
        /// <see cref="CreateEntity"/>.</param>
        [JsonConstructor]
        public EntityInfo(string id, object[] components)
        {
            (Id, Components) = (id, components);
        }

        /// <summary>
        /// Creates a new <see cref="Entity"/> instance for a provided <see cref="World"/> instance using the data
        /// assoicated with this <see cref="EntityInfo"/> instance.
        /// </summary>
        /// <param name="world"><see cref="World"/> instance to create the <see cref="Entity"/> instance.</param>
        /// <returns>The newly created <see cref="Entity"/> instance.</returns>
        public Entity CreateEntity(World world)
        {
            Entity outEntity = world.CreateEntity();

            foreach (var component in Components)
            {
                outEntity.Set(component);
            }

            return outEntity;
        }
    }
}