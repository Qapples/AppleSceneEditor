using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AppleSceneEditor.Extensions;
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
        private StackPanel _entityStackPanel;
        private IList<JsonObject> _entitiesJsonObjects; //represents all the json objects currently loaded in the scene

        private string _entityContents = "";
        private JsonObject? _entityJsonObject;
        private TextButton? _entityButton;

        public RemoveEntityCommand(string entityPath, World world, StackPanel entityStackPanel,
            IList<JsonObject> entitiesJsonObjects)
        {
            (_entityPath, _world, _entityStackPanel, _entitiesJsonObjects, Disposed) =
                (entityPath, world, entityStackPanel, entitiesJsonObjects, false);
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

            _entityJsonObject =
                _entitiesJsonObjects.FirstOrDefault(o => o.FindProperty("id")?.Value is string value && value == id);

            if (_entityJsonObject is not null)
            {
                _entitiesJsonObjects.Remove(_entityJsonObject);
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

            _entityButton = _entityStackPanel.TryFindWidgetById<TextButton>($"EntityButton_{id}");

            if (_entityButton is not null)
            {
                _entityStackPanel.RemoveChild(_entityButton);
            }
            else
            {
                Debug.WriteLine($"{methodName}: cannot find entity button of id \"{id}\"");
            }
        }

        public void Undo()
        {
#if DEBUG
            const string methodName = nameof(RemoveEntityCommand) + "." + nameof(Execute);
#endif
            File.WriteAllText(_entityPath, _entityContents);

            if (_entityJsonObject is not null)
            {
                _entitiesJsonObjects.Add(_entityJsonObject);
            }

            Entity? revivedEntity = _entityJsonObject?.GenerateEntity(_world);
            if (revivedEntity is null)
            {
                Debug.WriteLine($"{methodName}: cannot create entity from world!");
            }

            if (_entityButton is not null)
            {
                _entityStackPanel.AddChild(_entityButton);
            }
        }

        public void Redo() => Execute();

        public void Dispose()
        {
            (_world, _entityStackPanel, _entitiesJsonObjects, _entityJsonObject, _entityButton, Disposed) =
                (null!, null!, null!, null!, null!, true);
        }
    }
}