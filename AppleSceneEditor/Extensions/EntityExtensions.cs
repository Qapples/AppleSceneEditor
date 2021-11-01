using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AppleSerialization.Info;
using AppleSerialization.Json;
using DefaultEcs;
using GrappleFightNET5.Scenes;

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
        /// <param name="scene">The <see cref="Scene"/> that provides the <see cref="World"/> instance that is used
        /// to create the entity.</param>
        /// <param name="entityPath">If this value is set to, then the contents of <see cref="rootObject"/> are written
        /// to this file path.</param>
        /// <param name="readerOptions">Optional <see cref="JsonReaderOptions"/> instance that determines how the
        /// contents of <see cref="rootObject"/> are read. If null, then a default value will be used.</param>
        /// <param name="serializerOptions">Optional <see cref="JsonSerializerOptions"/> instance that determines how
        /// the contents of <see cref="rootObject"/> are used to create an instance of <see cref="EntityInfo"/> If null,
        /// then a default value will be used.</param>
        /// <returns><see cref="Nullable{Entity}"/> is returned. If generation is successful, then it contains the
        /// <see cref="Entity"/>. Otherwise, it contains null.</returns>
        public static Entity? GenerateEntity(this JsonObject rootObject, Scene scene, string? entityPath = null,
            JsonReaderOptions? readerOptions = null, JsonSerializerOptions? serializerOptions = null)
        {
            //remove any entities with the same id beforehand as we are going to replace it.
            //cant use a reference because we are disposing the object if we find it and that causes unusual behavior.
            foreach (Entity entity in scene.Entities.GetEntities())
            {
                if (entity.Get<string>() == rootObject.Name) entity.Dispose();
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

            Entity outEntity = scene.World.CreateEntity();

            foreach (object component in entityInfo.Components)
            {
                Type componentType = component.GetType();

                //this right here is wasteful of memory. not a huge deal though lol.
                SetMethod.MakeGenericMethod(componentType).Invoke(outEntity, new[] {component});
            }
            
            outEntity.Set(entityInfo.Id);

            return outEntity;
        }
    }
}