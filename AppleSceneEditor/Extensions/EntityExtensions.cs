using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AppleSceneEditor.ComponentFlags;
using AppleSerialization.Info;
using AppleSerialization.Json;
using DefaultEcs;
using GrappleFight.Runtime;

namespace AppleSceneEditor.Extensions
{
    /// <summary>
    /// Provides additional methods for working with anything involving <see cref="Entity"/> instances.
    /// </summary>
    public static class EntityExtensions
    {
        private static readonly JsonReaderOptions DefaultJsonReaderOptions = new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        private static readonly JsonSerializerOptions DefaultJsonSerializationOptions = new()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private static readonly MethodInfo SetMethod =
            typeof(Entity).GetMethods().First(e => e.Name == "Set" && e.GetParameters().Length > 0);

        /// <summary>
        /// Given a <see cref="Scene"/>, generates an <see cref="Entity"/> from a <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="rootObject">The <see cref="JsonObject"/> to create the <see cref="Entity"/> instance from.
        /// </param>
        /// <param name="world">The <see cref="World"/> instance that tne entity will be created from.</param>
        /// <param name="entityPath">If this value is set to, then the contents of <see cref="rootObject"/> are written
        /// to this file path.</param>
        /// <param name="readerOptions">Optional <see cref="JsonReaderOptions"/> instance that determines how the
        /// contents of <see cref="rootObject"/> are read. If null, then a default value will be used.</param>
        /// <param name="serializerOptions">Optional <see cref="JsonSerializerOptions"/> instance that determines how
        /// the contents of <see cref="rootObject"/> are used to create an instance of <see cref="EntityInfo"/> If null,
        /// then a default value will be used.</param>
        /// <returns><see cref="Nullable{Entity}"/> is returned. If generation is successful, then it contains the
        /// <see cref="Entity"/>. Otherwise, it contains null.</returns>
        public static Entity? GenerateEntity(this JsonObject rootObject, World world, string? entityPath = null,
            JsonReaderOptions? readerOptions = null, JsonSerializerOptions? serializerOptions = null)
        {
            
            //if we have a selected entity flag, make sure that we transfer that flag over to the new entity
            bool isSelectedEntity = false;

            //remove any entities with the same id beforehand as we are going to replace it.
            //cant use a reference because we are disposing the object if we find it and that causes unusual behavior.
            foreach (Entity entity in world.GetEntities().With<string>().AsEnumerable())
            {
                if (entity.Get<string>() == rootObject.Name)
                {
                    //is this the selected entity flag?
                    if (world.Has<SelectedEntityFlag>() && world.Get<SelectedEntityFlag>().SelectedEntity == entity)
                    {
                        isSelectedEntity = true;
                    }

                    entity.Dispose();

                    //there should only be one match
                    break;
                }
            }

            string objectContents = rootObject.GenerateJsonText();
            if (entityPath is not null) File.WriteAllText(entityPath, objectContents);

            Utf8JsonReader reader =
                new(Encoding.UTF8.GetBytes(objectContents), readerOptions ?? DefaultJsonReaderOptions);
            EntityInfo? entityInfo =
                EntityInfo.Deserialize(ref reader, serializerOptions ?? DefaultJsonSerializationOptions);

            if (entityInfo is null)
            {
                Debug.WriteLine($"{nameof(GenerateEntity)}: cannot deserialize into EntityInfo.");
                return null;
            }

            Entity outEntity = world.CreateEntity();

            foreach (object component in entityInfo.Components)
            {
                Type componentType = component.GetType();

                //this right here is wasteful of memory. not a huge deal though lol.
                SetMethod.MakeGenericMethod(componentType).Invoke(outEntity, new[] {component});
            }
            
            outEntity.Set(entityInfo.Id);

            if (isSelectedEntity)
            {
                world.Set(new SelectedEntityFlag(outEntity));
            }

            return outEntity;
        }
        
        public static bool TryGetEntityById(Scene scene, string entityId, out Entity entity)
        {
            try
            {
                entity = scene.EntityMap[entityId];
                return true;
            }
            catch
            {
                entity = new Entity();
                return false;
            }
        }
    }
}