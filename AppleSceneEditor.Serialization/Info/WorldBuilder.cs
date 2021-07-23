using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DefaultEcs;
using DefaultEcs.Serialization;

namespace AppleSceneEditor.Serialization.Info
{
    /// <summary>
    /// Used to create <see cref="World"/> instances with <see cref="EntityInfo"/> instances.
    /// </summary>
    public class WorldBuilder
    {
        public List<EntityInfo> Entities { get; init; }

        private readonly JsonReaderOptions _defaultJsonReaderOptions = new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        private readonly JsonSerializerOptions _defaultJsonSerializationOptions = new()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public WorldBuilder(List<EntityInfo>? entities = null)
        {
            Entities = entities ?? new List<EntityInfo>();
        }

        /// <summary>
        /// Adds an entity through the serialization of .json text that describes the components of an entity.
        /// </summary>
        /// <param name="entityContent">The contents of json text that describe the components of that entity along
        /// with an ID. (Note: this is the data of the json itself! Not the file name. Use
        /// <see cref="File.ReadAllText(string)"/> to get the contents of a file.</param>
        /// <param name="jsonReaderOptions"><see cref="JsonReaderOptions"/> instance that determines how the data from
        /// <see cref="entityContent"/> is read. This parameter is optional and a default value will be used instead if
        /// this parameter is not set to or is null.</param>
        /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> instance that determines how the
        /// data from <see cref="entityContent"/> is serialized. This parameter is optional and a default value will
        /// be used instead if this parameter is not set to or is null.</param>
        public void AddEntity(string entityContent, JsonReaderOptions? jsonReaderOptions = null,
            JsonSerializerOptions? jsonSerializerOptions = null) =>
            AddEntities(new[] {entityContent}, jsonReaderOptions, jsonSerializerOptions);

        /// <summary>
        /// Adds an entity through the serialization of numerous instances of .json text that describes the
        /// components of entities.
        /// </summary>
        /// <param name="entityContents">A collection of json text that describe the components of entities along
        /// with their IDs. (Note: this is the data of the json itself! Not the file name. Use
        /// <see cref="File.ReadAllText(string)"/> to get the contents of a file.</param>
        /// <param name="jsonReaderOptions"><see cref="JsonReaderOptions"/> instance that determines how the data from
        /// <see cref="entityContents"/> is read. This parameter is optional and a default value will be used instead if
        /// this parameter is not set to or is null.</param>
        /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> instance that determines how the
        /// data from <see cref="entityContents"/> is serialized. This parameter is optional and a default value will
        /// be used instead if this parameter is not set to or is null.</param>
        public void AddEntities(IEnumerable<string> entityContents, JsonReaderOptions? jsonReaderOptions = null,
            JsonSerializerOptions? jsonSerializerOptions = null)
        {
            foreach (string contents in entityContents)
            {
                Utf8JsonReader reader =
                    new(Encoding.UTF8.GetBytes(contents), jsonReaderOptions ?? _defaultJsonReaderOptions);

                EntityInfo? entityInfo = EntityInfo.Deserialize(ref reader,
                    jsonSerializerOptions ?? _defaultJsonSerializationOptions);
                if (entityInfo is not null) Entities.Add(entityInfo);
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="World"/> using the entity data provided by <see cref="Entities"/>.
        /// </summary>
        /// <returns>A <see cref="World"/> instance with the entities described by <see cref="EntityInfo"/>.</returns>
        public World CreateWorld()
        {
            World outputWorld = new();
            MethodInfo setMethod =
                typeof(Entity).GetMethods().First(e => e.Name == "Set" && e.GetParameters().Length > 0);

            foreach (EntityInfo info in Entities)
            {
                Entity entity = outputWorld.CreateEntity();

                foreach (object component in info.Components)
                {
                    Type componentType = component.GetType();

                    setMethod.MakeGenericMethod(componentType).Invoke(entity, new[] {component});
                }
            }

            return outputWorld;
        }
    }
}