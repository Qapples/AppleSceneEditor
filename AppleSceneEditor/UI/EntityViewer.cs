using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AppleSceneEditor.Commands;
using AppleSceneEditor.ComponentFlags;
using AppleSceneEditor.Extensions;
using AppleSerialization.Json;
using DefaultEcs;
using GrappleFight.Resource.Info;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.UI
{
    public class EntityViewer : IDisposable
    {
        public const string EntityGridIdPrefix = "EntityGrid_";

        public const string EntityDropDownGridId = "DropDownGrid";
        public const string WidgetStackPanelName = "WidgetStackPanel";
        public const string EntityButtonName = "EntityBytton";

        public string EntitiesDirectory { get; private set; }
                
        public World World { get; private set; }

        public StackPanel EntityButtonStackPanel { get; private set; }

        public List<JsonObject> EntityJsonObjects { get; private set; }

        private CommandStream _commands;
        private Window _addEntityWindow;
        private EntityMap<string> _entityIdMap;
        private bool _disposed;

        private string? _makeChildEntityName;
        private TextButton? _previousChildButton;

        public EntityViewer(string entitiesDirectory, World world, StackPanel buttonStackPanel,
            List<JsonObject> entityJsonObjects, CommandStream commands, Desktop desktop)
        {
            (EntitiesDirectory, World, EntityButtonStackPanel, EntityJsonObjects, _commands) =
                (entitiesDirectory, world, buttonStackPanel, entityJsonObjects, commands);
            
            _entityIdMap = world.GetEntities().With<string>().AsMap<string>();
            _addEntityWindow = CreateAddNewEntityWindow(entitiesDirectory);
            
            PopulatePanel(entitiesDirectory);
            InitAddEntityButtonEvent(desktop);
        }
        
        //Actual adding/removing logic is within the commands themselves since it's easier to undo
        //that way since you can cache the results of the execution of the command within the command object

        public void AddEntity(string id, string entityContents)
        {
            string entityPath = Path.Combine(EntitiesDirectory, $"{id}.entity");
            _commands.AddCommandAndExecute(new AddEntityCommand(entityPath, entityContents, World));

            ref var flag = ref World.Get<AddedEntityFlag>();
            CreateEntityButtonGrid(id, flag.AddedEntity, out _);
            EntityJsonObjects.Add(flag.AddedJsonObject);
            
            World.Remove<AddedEntityFlag>();
        }

        public void RemoveEntity(string id)
        {
            string entityPath = Path.Combine(EntitiesDirectory, $"{id}.entity");
            _commands.AddCommandAndExecute(new RemoveEntityCommand(entityPath, World));

            RemoveEntityButtonGrid(id);
            
            JsonObject? foundJsonObject = EntityJsonObjects.FindJsonObjectById(id);
            if (foundJsonObject is not null)
            {
                EntityJsonObjects.Remove(foundJsonObject);
            }
            
            World.Remove<RemovedEntityFlag>();
        }

        public Grid CreateEntityButtonGrid(string id, Entity entity, out bool alreadyExists)
        {
#if DEBUG
            const string methodName = nameof(EntityViewer) + "." + nameof(CreateEntityButtonGrid);
#endif
            //Using FindWidgetById here instead of the Try version since we want to catch ANY widget regardless of it's
            //it if it has the same ID that we have.
            Widget foundGrid = EntityButtonStackPanel.FindWidgetById($"{EntityGridIdPrefix}{id}");
            if (foundGrid is not null)
            {
                Debug.WriteLine($"{methodName}: entity with id {id} already exists within the stack panel.");
                alreadyExists = true;
                return (Grid) foundGrid;
            }

            TextButton entityButton = new()
            {
                Text = id, 
                Id = EntityButtonName,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            
            entityButton.Click += (_, _) =>
            {
                if (_makeChildEntityName == id)
                {
                    _makeChildEntityName = null;
                    return;
                }
                
                if (_makeChildEntityName is null)
                {
                    GlobalFlag.SetFlag(GlobalFlags.EntitySelected, true);
                    World.Set(new SelectedEntityFlag(entity));
                }
                else
                {
                    JsonObject? childJsonObject = EntityJsonObjects.FindJsonObjectById(_makeChildEntityName);

                    if (childJsonObject is not null)
                    {
                        _commands.AddCommandAndExecute(new AssignParentToEntityCommand(entity.Get<string>(),
                            _makeChildEntityName, childJsonObject, this));
                    }
                }

                _makeChildEntityName = null;
            };

            Grid dropDownGrid = MyraExtensions.CreateDropDown(new VerticalStackPanel {Id = WidgetStackPanelName},
                entityButton, $"{EntityGridIdPrefix}{id}");

            HorizontalStackPanel buttonStack = new()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                GridColumn = 1,
                Spacing = 10
            };
            
            TextButton makeChildButton = new()
            {
                Text = "Child",
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(3, 0)
            };

            TextButton removeButton = new()
            {
                Text = "-",
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(3, 0),
            };

            removeButton.Click += (_, _) => RemoveEntity(id);
            makeChildButton.Click += (_, _) =>
            {
                if (_makeChildEntityName == id)
                {
                    _makeChildEntityName = null;
                    makeChildButton.IsPressed = false;
                    _previousChildButton = null;
                }
                else
                {
                    _makeChildEntityName = id;
                    makeChildButton.IsPressed = true;

                    if (_previousChildButton is not null)
                    {
                        _previousChildButton.IsPressed = false;
                    }
                    
                    _previousChildButton = makeChildButton;
                }
            };

            entityButton.Click += (_, _) =>
            {
                makeChildButton.IsPressed = false;

                if (_previousChildButton is not null)
                {
                    _previousChildButton.IsPressed = false;
                }
            };

            buttonStack.AddChild(makeChildButton);
            buttonStack.AddChild(removeButton);
            
            dropDownGrid.AddChild(buttonStack);

            EntityButtonStackPanel.AddChild(dropDownGrid);

            alreadyExists = false;

            return dropDownGrid;
        }

        public Grid? RemoveEntityButtonGrid(string id)
        {
            Widget? widget = EntityButtonStackPanel.FindWidgetById($"{EntityGridIdPrefix}{id}");
            widget?.RemoveFromParent();

            return widget as Grid;
        }

        public void Dispose()
        {
            (World, EntityButtonStackPanel, EntityJsonObjects, _commands, _addEntityWindow, _entityIdMap, _disposed) =
                (null!, null!, null!, null!, null!, null!, true);
        }

        private void PopulatePanel(string entitiesDirectory)
        {
            EntityButtonStackPanel.Widgets.Clear();
            EntityButtonStackPanel.AddChild(new Label {Text = "Holder"});

            Dictionary<string, Grid> idButtonGridDict = new();
            Dictionary<string, string> parentDict = new();

            foreach (string entityPath in Directory.GetFiles(entitiesDirectory)
                .Where(f => Path.GetExtension(f) == ".entity"))
            {
                string id = Path.GetFileNameWithoutExtension(entityPath);

                if (_entityIdMap.ContainsKey(id))
                {
                    idButtonGridDict[id] = CreateEntityButtonGrid(id, _entityIdMap[id], out _);

                    string? parentId = GetParentIdFromEntityContents(File.ReadAllText(entityPath));
                    if (parentId is not null)
                    {
                        parentDict[id] = parentId;
                    }
                }
            }

            foreach (var (id, parentId) in parentDict)
            {
                VerticalStackPanel? parentWidgetContainer = idButtonGridDict[parentId]
                    .TryFindWidgetById<VerticalStackPanel>(WidgetStackPanelName);
                if (parentWidgetContainer is null) continue;

                Grid buttonGrid = idButtonGridDict[id];
                buttonGrid.RemoveFromParent();
                parentWidgetContainer.Widgets.Add(buttonGrid);
            }
        }

        //TODO: WARNING! If ParentInfo is modified (i.e. "parentID" is renamed to "parentName") then this regex will stop working!!!!
        private static readonly Regex GetParentIdRegex =
            new($@"""\$type"":\W*""{nameof(ParentInfo)}"",$\W*""parentId"":\W*""(.+)""", RegexOptions.Multiline);

        //see above comment on GetParentIdRegex
        private string? GetParentIdFromEntityContents(string entityContents)
        {
#if DEBUG
            const string methodName = nameof(EntityViewer) + "." + nameof(GetParentIdFromEntityContents);
#endif
            MatchCollection matches = GetParentIdRegex.Matches(entityContents);

            if (matches.Count > 1)
            {
                Debug.WriteLine($"{methodName} (WARNING): one more match for parentId is found!");
            }

            return matches.Count == 0 ? null : matches[0].Groups[1].Value;
        }

        private void InitAddEntityButtonEvent(Desktop desktop)
        {
            TextButton addEntityButton =
                (TextButton) EntityButtonStackPanel.Desktop.Root.FindWidgetById("AddEntityButton");

            addEntityButton.Click += (_, _) => _addEntityWindow?.ShowModal(desktop);
        }
        
        private Window CreateAddNewEntityWindow(string entitiesDirectory)
        {
            VerticalStackPanel stackPanel = new();
            Window outWindow = new() {Content = stackPanel};

            TextBox idTextBox = new()
            {
                Text = "", MinWidth = 250, HorizontalAlignment = HorizontalAlignment.Center
            };
            
            TextButton okButton = new() {Text = "OK", HorizontalAlignment = HorizontalAlignment.Right};
            TextButton cancelButton = new() {Text = "Cancel", HorizontalAlignment = HorizontalAlignment.Right};

            okButton.Click += (_, _) =>
            {
                NewEntityOkClick(Path.Combine(entitiesDirectory, $"{idTextBox.Text}.entity"));
                idTextBox.Text = "";
                outWindow.Close();
            };
            cancelButton.Click += (_, _) => outWindow.Close();

            stackPanel.AddChild(new Label
                {Text = "Enter the ID of the new entity", HorizontalAlignment = HorizontalAlignment.Center});
            stackPanel.AddChild(idTextBox);
            stackPanel.AddChild(new HorizontalStackPanel
                {Widgets = {okButton, cancelButton}, HorizontalAlignment = HorizontalAlignment.Right});

            return outWindow;
        }

        private void NewEntityOkClick(string entityPath)
        {
#if DEBUG
            const string methodName = nameof(EntityViewer) + "." + nameof(NewEntityOkClick);
#endif
            string? entityDirectory = Path.GetDirectoryName(entityPath);
            if (entityDirectory is null || !Directory.Exists(entityDirectory))
            {
                Debug.WriteLine($"{methodName}: failed to get directory of entity.");
                return;
            }
            
            //Directory.GetFiles returns the full path, not just the name of the files.
            if (Directory.GetFiles(entityDirectory).Any(s => Path.GetFullPath(s) == Path.GetFullPath(entityPath)))
            {
                Debug.WriteLine(
                    $"{methodName}: the entity file {entityPath} already exists!");
                return;   
            }
            
            string id = Path.GetFileNameWithoutExtension(entityPath);
            AddEntity(id, GenerateBlankEntityFile(id));
        }

        private static string GenerateBlankEntityFile(string entityId) => $@"{{
    ""components"": [
        {{
            ""$type"": ""{nameof(TransformInfo)}"",
            ""position"": ""0 0 0"",
            ""scale"": ""1 1 1"",
            ""rotation"": ""0 0 0"",
            ""velocity"": ""0 0 0""
        }}
    ],
    ""id"" : ""{entityId}""
}}";
    }
}