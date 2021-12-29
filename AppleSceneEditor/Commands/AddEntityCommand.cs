using System;
using System.Diagnostics;
using System.IO;
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
    public class AddEntityCommand : IEditorCommand
    {
        public bool Disposed { get; private set; }
        
        private readonly string _entityPath;
        private readonly string _entityContents;

        private World _world;
     
        private Entity _newEntity;
        private JsonObject _newEntityJsonObject;

        public AddEntityCommand(string entityPath, string entityContents, World world)
        {
            (_entityPath, _entityContents, _world, Disposed) =
                (entityPath, entityContents, world, false);

            _newEntity = default;
            _newEntityJsonObject = new JsonObject();
        }

        public void Execute()
        {
#if DEBUG
            const string methodName = nameof(AddEntityCommand) + "." + nameof(Execute);
#endif
            /* To fully add an entity, we must:
             * 1. Create the file of the entity within the entities directory. (.entity)
             * 2. Create a JsonObject instance from that new entity file.
             * 3. Create a new entity instance from the world.
             * 4. Add a button in EntityStackPanel so the user can select the entity within the entity viewer
             * 5. Add that JsonObject to a list of JsonObjects representative of each entity in the loaded scene.
             */

            string id = Path.GetFileNameWithoutExtension(_entityPath);

            File.WriteAllText(_entityPath, _entityContents);

            Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(_entityContents));
            
            JsonObject? nullableJsonObj = JsonObject.CreateFromJsonReader(ref reader);
            if (nullableJsonObj is null)
            {
                Debug.WriteLine($"{methodName}: unable to create a JsonObject from entity file: {_entityPath}");
                return;
            }

            _newEntityJsonObject = nullableJsonObj;
            nullableJsonObj = null;

            Entity? newEntityNullable = _newEntityJsonObject.GenerateEntity(_world);
            if (newEntityNullable is null)
            {
                Debug.WriteLine($"{methodName}: failed to create entity from JsonObject from file: {_entityPath}");
                return;
            }
            
            _world.Set(new AddedEntityFlag(newEntityNullable.Value, _newEntityJsonObject));
        }

        public void Undo()
        {
#if DEBUG
            const string methodName = nameof(AddEntityCommand) + "." + nameof(Undo);
#endif
            if (_newEntity != default)
            {
                _newEntity.Dispose();
            }

            string id = Path.GetFileNameWithoutExtension(_entityPath);

            try
            {
                File.Delete(_entityPath);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{methodName}: failed to remove entity file {_entityPath} with exception: {e}");
                return;
            }
            
            _world.Set(new RemovedEntityFlag(id));
        }

        public void Redo() => Execute();

        public void Dispose()
        {
            (_world, _newEntityJsonObject, Disposed) = (null!, null!, true);
        }
    }
}