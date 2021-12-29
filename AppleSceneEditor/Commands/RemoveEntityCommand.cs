using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AppleSceneEditor.ComponentFlags;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.UI;
using AppleSerialization.Json;
using DefaultEcs;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Commands
{
    public class RemoveEntityCommand : IEditorCommand
    {
        public bool Disposed { get; private set; }

        private readonly string _entityPath;
        
        private World _world;

        private string _entityContents = "";
        private JsonObject? _entityJsonObject;
        private Grid? _entityButtonGrid;

        public RemoveEntityCommand(string entityPath, World world)
        {
            (_entityPath, _world, Disposed) = (entityPath, world, false);
        }

        public void Execute()
        {
#if DEBUG
            const string methodName = nameof(RemoveEntityCommand) + "." + nameof(Execute);
#endif
            /* To fully delete an entity, we must:
                * 1. Delete the entity file (.entity) in the entities directory
                * 2. Remove the entity's JsonObject from the list of JsonObjects that contains all the entities in the scene
                * 3. Remove the entity from the world.
                * 4. Remove the entity's button in the EntityStackPanel
            */

            string id = Path.GetFileNameWithoutExtension(_entityPath);

            if (File.Exists(_entityPath))
            {
                _entityContents = File.ReadAllText(_entityPath);
                File.Delete(_entityPath);
            }

            foreach (Entity entity in _world.GetEntities().With<string>().AsEnumerable())
            {
                if (entity.Has<string>() && entity.Get<string>() == id)
                {
                    entity.Dispose();
                }
            }
            
            _world.Set(new RemovedEntityFlag(id));
        }

        public void Undo()
        {
#if DEBUG
            const string methodName = nameof(RemoveEntityCommand) + "." + nameof(Undo);
#endif
            File.WriteAllText(_entityPath, _entityContents);
            
            Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(_entityContents));
            
            JsonObject? nullableJsonObj = JsonObject.CreateFromJsonReader(ref reader);
            if (nullableJsonObj is null)
            {
                Debug.WriteLine($"{methodName}: unable to create a JsonObject from entity file: {_entityPath}");
                return;
            }

            Entity? revivedEntity = nullableJsonObj.GenerateEntity(_world);
            if (revivedEntity is null)
            {
                Debug.WriteLine($"{methodName}: cannot create entity from world!");
                return;
            }
            
            _world.Set(new AddedEntityFlag(revivedEntity.Value, nullableJsonObj));
        }

        public void Redo() => Execute();

        public void Dispose()
        {
            (_world, _entityJsonObject, _entityButtonGrid, Disposed) =
                (null!, null!, null!, true);
        }
    }
}