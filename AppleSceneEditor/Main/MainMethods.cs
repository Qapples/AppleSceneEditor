using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Input;
using AppleSceneEditor.Input.Commands;
using AppleSceneEditor.Exceptions;
using AppleSceneEditor.Systems;
using AppleSerialization.Json;
using DefaultEcs;
using GrappleFightNET5.Scenes;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using Myra.Utility;

//has to be in the AppleSceneEditor namespace for it to be a partial class of MainGame which is in that namespace
namespace AppleSceneEditor
{
    //we separated the methods into a different file as both the main game logic (loading, updating, drawing, etc.) and
    //the methods used to assist it (the methods declared here) are both going to be pretty large.
    
    public partial class MainGame
    {
        //----------------
        // Init methods
        //----------------

        private void InitUIFromScene(Scene scene)
        {
            //there should be a stack panel with an id of "EntityStackPanel" that should contain the entities. if it
            //exists and is a valid VerticalStackPanel, add the entities to the stack panel as buttons with their ID.
            bool result = _desktop.Root.ProcessWidgets(widget =>
            {
                if (widget.Id == "MainGrid")
                {
                    if (widget is not Grid grid) return false;

                    ScrollViewer? scrollViewer = grid.TryFindWidgetById<ScrollViewer>("EntityScrollViewer");
                    if (scrollViewer is null)
                    {
                        Debug.WriteLine("Can't find scrollviewer of ID \"EntityScrollViewer\"");
                        return false;
                    }

                    VerticalStackPanel? stackPanel =
                        scrollViewer.TryFindWidgetById<VerticalStackPanel>("EntityStackPanel");
                    if (stackPanel is null)
                    {
                        Debug.WriteLine("Can't find VerticalStackPanel with ID of \"EntityStackPanel\".");
                        return false;
                    }

                    foreach (ref readonly var entity in scene.Entities.GetEntities())
                    {
                        if (!entity.Has<string>()) continue;

                        //can't use ref due to closure
                        var id = entity.Get<string>();

                        TextButton button = new() {Text = id, Id = "EntityButton_" + id};
                        button.TouchDown += (o, e) => UpdatePropertyGridWithEntity(scene, id);

                        stackPanel.AddChild(button);
                    }
                    
                    //init scene with base entity
                    UpdatePropertyGridWithEntity(scene, "Base");
                    
                    //init _inputHelper here since by then all the fields should have been initialized so far.
                    _inputHandler = CreateInputHandlerFromFile(Path.Combine(_configPath, "Keybinds.txt"));
                }

                return true;
            });

            if (!result)
            {
                Debug.WriteLine("Error was encountered in InitUIFromScene. Use debugger.");
            }
        }

        //-----------------------------
        // UI window creation methods
        //-----------------------------
        
        private Window CreateNewComponentDialog()
        {
            Panel panel = new();
            Window outWindow = new() {Content = panel};
            VerticalStackPanel stackPanel = new();

            ComboBox typeSelectionBox = new() {HorizontalAlignment = HorizontalAlignment.Center};
            foreach (string type in _prototypes.Keys)
            {
                typeSelectionBox.Items.Add(new ListItem {Text = type});
            }

            TextButton okButton = new() {Text = "OK", HorizontalAlignment = HorizontalAlignment.Right};
            TextButton cancelButton = new() {Text = "Cancel", HorizontalAlignment = HorizontalAlignment.Right};
            
            okButton.Click += (o, e) => FinishButtonClick(typeSelectionBox.SelectedItem.Text);
            cancelButton.Click += (o, e) => outWindow.Close();

            stackPanel.AddChild(new Label
                {Text = "Select type of component", HorizontalAlignment = HorizontalAlignment.Center});
            stackPanel.AddChild(typeSelectionBox);
            stackPanel.AddChild(new HorizontalStackPanel
                {Widgets = {okButton, cancelButton}, HorizontalAlignment = HorizontalAlignment.Right});
            
            panel.Widgets.Add(stackPanel);

            return outWindow;
        }

        private Window CreateAlreadyExistsDialog()
        {
            Panel panel = new();
            Window outWindow = new() {Content = panel};
            VerticalStackPanel stackPanel = new();

            stackPanel.AddChild(new Label
            {
                Text = "Cannot add component because another component of the same type already exists!",
                StyleName = "small",
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            
            TextButton okButton = new() {Text = "OK", HorizontalAlignment = HorizontalAlignment.Right};
            okButton.Click += (o, e) => outWindow.Close();

            stackPanel.AddChild(okButton);
            
            panel.Widgets.Add(stackPanel);
            
            return outWindow;
        }

        private FileDialog CreateOpenFileDialog()
        {
            FileDialog fileDialog = new(FileDialogMode.ChooseFolder) {Enabled = true, Visible = true};
            
            fileDialog.Closed += (o, e) =>
            {
                if (!fileDialog.Result) return;

                string filePath = fileDialog.FilePath;
                if (string.IsNullOrEmpty(filePath)) return;

                _currentScene = new Scene(Directory.GetParent(filePath)!.FullName, GraphicsDevice, null, _spriteBatch,
                    true);
                _jsonObjects = IOHelper.CreateJsonObjectsFromScene(Directory.GetParent(filePath)!.FullName);

                if (_currentScene is not null)
                {
                    InitUIFromScene(_currentScene);
                    
                    _drawSystem?.Dispose();
                    _drawSystem = new DrawSystem(_currentScene.World, GraphicsDevice);
                }
            };

            return fileDialog;
        }

        private FileDialog CreateNewFileDialog()
        {
            FileDialog fileDialog = new(FileDialogMode.OpenFile) {Filter = "*.world", Enabled = true, Visible = true};
            
            fileDialog.Closed += (o, e) =>
            {
                if (!fileDialog.Result) return;

                string folderPath = fileDialog.Folder;
                if (string.IsNullOrEmpty(folderPath)) return;

                _currentScene = IOHelper.CreateNewScene(folderPath, _spriteBatch);
                _jsonObjects = IOHelper.CreateJsonObjectsFromScene(folderPath);

                if (_currentScene is not null)
                {
                    InitUIFromScene(_currentScene);
                    
                    _drawSystem?.Dispose();
                    _drawSystem = new DrawSystem(_currentScene.World, GraphicsDevice);
                }
            };

            return fileDialog;
        }

        //---------------
        // Update methods
        //---------------

        private ComponentPanelHandler? _mainPanelHandler;

        /// <summary>
        /// Updates the properties viewer to display the components/properties of an entity of a specified ID.
        /// </summary>
        /// <param name="scene">The <see cref="Scene"/> instance whose <see cref="Scene.World"/> is where the desired
        /// entity belongs to.</param>
        /// <param name="entityId">The ID of the <see cref="Entity"/> to display it's components.</param>
        private void UpdatePropertyGridWithEntity(Scene scene, string entityId)
        {
            const string methodName = nameof(MainGame) + "." + nameof(UpdatePropertyGridWithEntity);
            
            _desktop.Root.ProcessWidgets(widget =>
            {
                if (widget.Id == "MainGrid")
                {
                    if (widget is not Grid grid) return false;
                    if (!EntityExtensions.TryGetEntityById(scene, entityId, out _)) return false;

                    StackPanel? propertyStackPanel = grid.TryFindWidgetById<StackPanel>("PropertyStackPanel");
                    if (propertyStackPanel is null) return false;

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
                        Debug.WriteLine($"{methodName}: Cannot find entity of Id {entityId} in _jsonObjects.");
                        return false;
                    }

                    _currentJsonObject = selectedJsonObject;
                    
                    if (_mainPanelHandler is null)
                    {
                        try
                        {
                            _mainPanelHandler =
                                new ComponentPanelHandler(_desktop, selectedJsonObject, propertyStackPanel, _commands);
                        }
                        catch (ComponentsNotFoundException e)
                        {
                            Debug.WriteLine(e);
                            return false;
                        }
                    }
                    else
                    {
                        try
                        {
                            _mainPanelHandler.RootObject = selectedJsonObject;
                        }
                        catch (ComponentsNotFoundException e)
                        {
                            Debug.WriteLine(e);
                            return false;
                        }
                    }
                }

                return true;
            });
        }

        //we can't have this method in InputHandler because it calls TryGetCommandFromFunctionName which uses private
        //fields from MainGame :(.
        private InputHandler CreateInputHandlerFromFile(string filePath)
        {
            const string methodName = nameof(MainGame) + "." + nameof(CreateInputHandlerFromFile);

            Dictionary<KeyboardState, IKeyCommand> commands = new();
            KeyboardState blankState = new();

            using StreamReader reader = new(filePath, Encoding.ASCII);

            string? line = reader.ReadLine();
            while (line is not null)
            {
                int colonIndex = line.IndexOf(':');
                
                if (colonIndex > 0)
                {
                    //there should only be ONE space between the colon and the key. account for the space and colon by
                    //adding two
                    string funcName = line[..colonIndex];
                    string keysStr = line[(colonIndex + 2)..];

                    if (TryGetCommandFromFunctionName(funcName, out var command))
                    {
                        KeyboardState state = KeyboardExtensions.ParseKeyboardState(keysStr);
                        if (state != blankState)
                        {
                            commands.Add(state, command);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"{methodName}: cannot get function name from following line: {line}");
                    }
                }
                else
                {
                    Debug.WriteLine($"{methodName}: cannot find func name behind colon in the following line of " +
                                    $"file with name of \"{filePath}\": {line}. Skipping.");
                }


                line = reader.ReadLine();
            }

            return new InputHandler(commands);
        }

        //------------------
        // TryGet methods
        //------------------

        //We could handle this via parsing an external file but imo the user should have no control over this so
        //we are hard coding this instead.
        private bool TryGetCommandFromFunctionName(string funcName, out IKeyCommand command) =>
            (command = funcName switch
            {
                "save" when _mainPanelHandler is not null && _currentScene is not null =>
                    new SaveCommand(_mainPanelHandler, _currentScene),
                "open" => new OpenCommand(this),
                "new" => new NewCommand(this),
                "undo" => new UndoCommand(_commands),
                "redo" => new RedoCommand(_commands),
                _ => new EmptyCommand()
            }) is not EmptyCommand;
    }
}