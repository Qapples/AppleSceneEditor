using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using AppleSerialization.Json;
using DefaultEcs;
using GrappleFightNET5.Scenes;
using Myra.Graphics2D.UI;
using Myra.Utility;

//has to be in the AppleSceneEditor namespace for it to be a partial class of MainGame which is in that namespace
namespace AppleSceneEditor
{
    public partial class MainGame
    {
        //-----------------------
        // ADDITIONAL METHODS
        //-----------------------

        private const string BaseEntityContents = @"{
    ""components"": [
        {
        }
    ],
    ""id"" : ""Base""
}";

        private void InitNewProject(string folderPath, int maxCapacity = 128)
        {
            string worldPath = Path.Combine(folderPath, new DirectoryInfo(folderPath).Name + ".world");

            //create paths
            string entitiesPath = Path.Combine(folderPath, "Entities");
            Directory.CreateDirectory(Path.Combine(folderPath, "Systems"));
            Directory.CreateDirectory(Path.Combine(folderPath, "Entities"));
            Directory.CreateDirectory(Path.Combine(folderPath, "Content"));

            //create world file
            using (StreamWriter writer = File.CreateText(worldPath))
            {
                writer.WriteLine("WorldMaxCapacity " + maxCapacity);
                writer.Flush();
            }

            //add base entity
            using (StreamWriter writer = File.CreateText(Path.Combine(entitiesPath, "BaseEntity")))
            {
                writer.WriteLine(BaseEntityContents);
                writer.Flush();
            }

            _currentScene = new Scene(folderPath, GraphicsDevice, null, _spriteBatch, true);
        }

        private void InitUIFromScene(Scene scene)
        {
            //there should be a stack panel with an id of "EntityStackPanel" that should contain the entities. if it
            //exists and is a valid VerticalStackPanel, add the entities to the stack panel as buttons with their ID.
            bool result = UIUtils.ProcessWidgets(_desktop.Root, widget =>
            {
                if (widget.Id == "MainGrid")
                {
                    if (widget is not Grid grid) return false;

                    ScrollViewer? scrollViewer = TryFindWidgetById<ScrollViewer>(grid, "EntityScrollViewer");
                    if (scrollViewer is null)
                    {
                        Debug.WriteLine("Can't find scrollviewer of ID \"EntityScrollViewer\"");
                        return false;
                    }

                    VerticalStackPanel? stackPanel =
                        TryFindWidgetById<VerticalStackPanel>(scrollViewer, "EntityStackPanel");
                    if (stackPanel is null)
                    {
                        Debug.WriteLine("Can't find VerticalStackPanel with ID of \"EntityStackPanel\".");
                        return false;
                    }

                    foreach (Entity entity in scene.Entities.GetEntities())
                    {
                        if (!entity.Has<string>()) continue;

                        //can't use ref due to closure
                        var id = entity.Get<string>();

                        TextButton button = new() {Text = id, Id = "EntityButton_" + id};
                        button.TouchDown += (o, e) => UpdatePropertyGridWithEntity(scene, id);

                        stackPanel.AddChild(button);
                    }
                }

                return true;
            });

            if (!result)
            {
                Debug.WriteLine("Error was encountered in InitUIFromScene. Use debugger.");
            }
        }

        private ComponentPanelHandler? _propertyGrid;

        private void UpdatePropertyGridWithEntity(Scene scene, string entityId)
        {
            _desktop.Root.ProcessWidgets(widget =>
            {
                if (widget.Id == "MainGrid")
                {
                    if (widget is not Grid grid) return false;
                    if (!TryGetEntityById(scene, entityId, out var entity)) return false;

                    StackPanel? nameStackPanel = TryFindWidgetById<StackPanel>(grid, "NameStackPanel");
                    StackPanel? valueStackPanel = TryFindWidgetById<StackPanel>(grid, "ValueStackPanel");

                    if (valueStackPanel is null || nameStackPanel is null) return false;

                    //MyraPad is stupid and trying to use PropertyGrids that are loaded through xml are pretty buggy,
                    //so we're gonna have to make a new ComponentPanelHandler on the spot.
                    //epic linq gaming
                    JsonObject? selectedJsonObject = (from obj in _jsonObjects
                        from prop in obj.Properties
                        where prop.Name.ToLower() == "id"
                        where prop.ValueKind == JsonValueKind.String
                        where string.Equals(prop.Value as string, entityId, StringComparison.CurrentCultureIgnoreCase)
                        select obj).FirstOrDefault();

                    if (selectedJsonObject is null)
                    {
                        //nameof just in case the name of the method changes
                        Debug.WriteLine($"Cannot find entity of Id {entityId} in _jsonObjects. Returning false" +
                                        $" ({nameof(UpdatePropertyGridWithEntity)})");
                        return false;
                    }

                    _currentJsonObject = selectedJsonObject;
                    
                    if (_propertyGrid is null)
                    {
                        _propertyGrid = new ComponentPanelHandler(selectedJsonObject, nameStackPanel, valueStackPanel);
                    }
                    else
                    {
                        _propertyGrid.RootObject = selectedJsonObject;
                    }
                }

                return true;
            });
        }

        private static T? TryFindWidgetById<T>(Container container, string id) where T : class
        {
            T? output;

            try
            {
                output = container.FindWidgetById(id) as T;

                if (output is null)
                {
                    Debug.WriteLine($"{id} cannot be casted into an instance of {typeof(T)}");
                    return null;
                }
            }
            catch
            {
                Debug.WriteLine($"{typeof(T)} of ID {id} cannot be found.");
                return null;
            }

            return output;
        }

        private void GetJsonObjectsFromScene(string scenePath)
        {
            string entitiesFolderPath = Path.Combine(scenePath, "Entities");

            if (!Directory.Exists(entitiesFolderPath)) return;

            foreach (string entityPath in Directory.GetFiles(entitiesFolderPath))
            {
                Utf8JsonReader reader = new(File.ReadAllBytes(entityPath), new JsonReaderOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
                
                _jsonObjects.Add(new JsonObject(ref reader));
            }
        }
        

        //We're using the "retrun bool and out" version of the "Try" method instead of using nullable because nullables
        //on value types tend to cause extra copies to be made since they're boxed in a special type for nullable value
        //types. tbh i dunno why i care so much about this LOL this project doesn't need any """optimization""" whatever
        //it's okay
        private static bool TryGetEntityById(Scene scene, string entityId, out Entity entity)
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