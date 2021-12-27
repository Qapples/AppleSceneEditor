using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private EntityViewer _viewer;
        
        private string _entityContents = "";
        private JsonObject? _entityJsonObject;
        private Grid? _entityButtonGrid;

        public RemoveEntityCommand(string entityPath, EntityViewer viewer)
        {
            (_entityPath, _world, _viewer, Disposed) =
                (entityPath, viewer.World, viewer, false);
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
            List<JsonObject> jsonObjects = _viewer.EntityJsonObjects;

            if (File.Exists(_entityPath))
            {
                _entityContents = File.ReadAllText(_entityPath);
                File.Delete(_entityPath);
            }

            _entityJsonObject =
                jsonObjects.FirstOrDefault(o => o.FindProperty("id")?.Value is string value && value == id);

            if (_entityJsonObject is not null)
            {
                jsonObjects.Remove(_entityJsonObject);
            }
            else
            {
                Debug.WriteLine($"{methodName}: cannot find entity of id \"{id}\" in lost of JsonObjects!");
            }

            foreach (Entity entity in _world.GetEntities().With<string>().AsEnumerable())
            {
                if (entity.Has<string>() && entity.Get<string>() == id)
                {
                    entity.Dispose();
                }
            }

            _entityButtonGrid = _viewer.RemoveEntityButtonGrid(id);
        }

        public void Undo()
        {
#if DEBUG
            const string methodName = nameof(RemoveEntityCommand) + "." + nameof(Execute);
#endif
            File.WriteAllText(_entityPath, _entityContents);

            if (_entityJsonObject is not null)
            {
                _viewer.EntityJsonObjects.Add(_entityJsonObject);
            }

            Entity? revivedEntity = _entityJsonObject?.GenerateEntity(_world);
            if (revivedEntity is null)
            {
                Debug.WriteLine($"{methodName}: cannot create entity from world!");
            }

            if (_entityButtonGrid is not null)
            {
                _viewer.EntityButtonStackPanel.AddChild(_entityButtonGrid);
            }
        }

        public void Redo() => Execute();

        public void Dispose()
        {
            (_world, _viewer, _entityJsonObject, _entityButtonGrid, Disposed) =
                (null!, null!, null!, null!, true);
        }
    }
}