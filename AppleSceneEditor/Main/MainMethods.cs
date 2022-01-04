using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AppleSceneEditor.Commands;
using AppleSceneEditor.ComponentFlags;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Input;
using AppleSceneEditor.Input.Commands;
using AppleSceneEditor.Exceptions;
using AppleSceneEditor.Systems;
using AppleSceneEditor.UI;
using AppleSerialization.Json;
using DefaultEcs;
using DefaultEcs.System;
using GrappleFightNET5.Components;
using GrappleFightNET5.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.Brushes;
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

        private Scene InitScene(string sceneDirectory)
        {
            Scene scene = new(sceneDirectory, GraphicsDevice, null, _spriteBatch, true);
            _jsonObjects = IOHelper.CreateJsonObjectsFromScene(sceneDirectory);

            scene.Compile();
            
            //initialize world-wide components
            scene.World.Set(new Camera
            {
                Position = Vector3.Zero,
                LookAt = new Vector3(0f, 0f, 20f),
                ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(95f),
                    GraphicsDevice.DisplayMode.AspectRatio, 1f, 1000f),
                Sensitivity = 2f
            });
                    
            scene.World.Set(new CameraProperties
            {
                YawDegrees = 0f,
                PitchDegrees = 0f,
                CameraSpeed = 0.5f
            });
            
            scene.World.Set(AxisType.Move);

            _commands.Dispose();
            _commands = new CommandStream();
            
            _drawSystems = new SequentialSystem<GameTime>(
                new EntityDrawSystem(scene.World, GraphicsDevice),
                new AxisDrawSystem(scene.World, GraphicsDevice, _commands));

            _entityViewer?.Dispose();
            _entityViewer = new EntityViewer(Path.Combine(sceneDirectory, "Entities"), scene.World,
                (StackPanel) _desktop.Root.FindWidgetById("EntityStackPanel"), _jsonObjects, _commands, _desktop);
            
            _mainPanelHandler?.Dispose();
            _mainPanelHandler = null;

            _fileViewer.CurrentDirectory = sceneDirectory;
            _fileViewer.World = scene.World;
            
            foreach (Entity entity in scene.Entities.GetEntities())
            {
                if (!entity.Has<string>()) continue;
                
                ref var id = ref entity.Get<string>();

                //the base entity should be selected by default
                if (string.Equals(id, "base", StringComparison.OrdinalIgnoreCase))
                {
                    scene.World.Set(new SelectedEntityFlag(entity));
                }
            }
            
            SelectEntity(scene, "Base");

            return scene;
        }

        //-----------------------------
        // UI window creation methods
        //-----------------------------

        private FileDialog CreateOpenFileDialog()
        {
            FileDialog fileDialog = new(FileDialogMode.ChooseFolder) {Enabled = true, Visible = true};
            
            fileDialog.Closed += (o, e) =>
            {
#if DEBUG
                const string methodName = nameof(CreateOpenFileDialog) + "." + nameof(fileDialog.Closed);
#endif
                if (!fileDialog.Result) return;

                string folder = fileDialog.Folder;
                if (string.IsNullOrEmpty(folder)) return;

                SceneValidationError error = Scene.ValidateSceneDirectory(folder);
                if (error != SceneValidationError.Valid)
                {
                    Debug.WriteLine($"{methodName}: scene directory ({folder}) is invalid with message: " +
                                    $"{error.ToErrorMessage()}");
                    return;
                }

                _currentScene?.Dispose();
                _currentScene = InitScene(folder);
                
                _notHeldInputHandler.Dispose();
                _heldInputHandler.Dispose();
                
                (_notHeldInputHandler, _heldInputHandler) =
                    CreateInputHandlersFromFile(Path.Combine(_configPath, "Keybinds.txt"));
            };

            return fileDialog;
        }

        private FileDialog CreateNewFileDialog()
        {
            FileDialog fileDialog = new(FileDialogMode.SaveFile) {Filter = "*.world", Enabled = true, Visible = true};
            
            fileDialog.Closed += (o, e) =>
            {
                if (!fileDialog.Result) return;

                //the returning name of the file regardless of the extension will be the scene name.
                if (string.IsNullOrEmpty(fileDialog.Folder) || string.IsNullOrEmpty(fileDialog.FilePath))
                {
                    return;
                }

                string scenePath = Path.Combine(Path.Combine(fileDialog.Folder,
                    Path.GetFileNameWithoutExtension(fileDialog.FilePath)));
                if (IOHelper.CreateNewScene(scenePath))
                {
                    _currentScene?.Dispose();
                    _currentScene = InitScene(scenePath);

                    _notHeldInputHandler.Dispose();
                    _heldInputHandler.Dispose();

                    (_notHeldInputHandler, _heldInputHandler) =
                        CreateInputHandlersFromFile(Path.Combine(_configPath, "Keybinds.txt"));
                }
            };

            return fileDialog;
        }

        //---------------
        // Update methods
        //---------------

        private ComponentPanelHandler? _mainPanelHandler;

        /// <summary>
        /// Selects the entity that has a specified ID. Selecting an entity involves: <br/>
        /// Updating the properties viewer to display the components/properties of an entity of a specified ID <br/>
        /// Highlighting the entity in the entity viewer and in the scene viewer <br/>
        /// Transformations will be applied to selected entity (if applicable)
        /// </summary>
        /// <param name="scene">The <see cref="Scene"/> instance whose <see cref="Scene.World"/> is where the desired
        /// entity belongs to.</param>
        /// <param name="entityId">The ID of the <see cref="Entity"/> to select.</param>
        private void SelectEntity(Scene scene, string entityId)
        {
            const string methodName = nameof(MainGame) + "." + nameof(SelectEntity);
            
            _desktop.Root.ProcessWidgets(widget =>
            {
                if (widget.Id == "MainGrid")
                {
                    if (widget is not Grid grid) return false;
                    if (!EntityExtensions.TryGetEntityById(scene, entityId, out _)) return false;

                    StackPanel? propertyStackPanel = grid.TryFindWidgetById<StackPanel>("PropertyStackPanel");
                    if (propertyStackPanel is null) return false;

                    propertyStackPanel.AcceptsKeyboardFocus = true;

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

                    //highlight the entity in the entity viewer
                    VerticalStackPanel? stackPanel =
                        grid.TryFindWidgetById<VerticalStackPanel>("EntityStackPanel");
                    if (stackPanel is null)
                    {
                        Debug.WriteLine("Can't find VerticalStackPanel with ID of \"EntityStackPanel\".");
                        return false;
                    }

                    foreach (Widget panelWidget in stackPanel.Widgets)
                    {
                        if (panelWidget is not TextButton button) continue;

                        int entityIdIndex = button.Id.IndexOf('_') + 1;
                        button.Background =
                            new SolidBrush(button.Id[entityIdIndex..] == entityId ? Color.SkyBlue : Color.Gray);
                    }
                }

                return true;
            });
        }

        //we can't have this method in InputHandler because it calls TryGetCommandFromFunctionName which uses private
        //fields from MainGame :(.
        public (InputHandler notHeldHandler, InputHandler heldHandler) CreateInputHandlersFromFile(string filePath)
        {
#if DEBUG
            const string methodName = nameof(MainGame) + "." + nameof(CreateInputHandlersFromFile);
#endif
            using StreamReader reader = new(filePath, Encoding.ASCII);

            InputHandler? notHeldHandler = null;
            InputHandler? heldHandler = null;

            //might be able to do this through regex but I'm too lazy to come up with a complicated regex query.
            string? line = reader.ReadLine();
            while (line is not null)
            {
                //# indicates a region. start looking for data after a region.
                if (!string.IsNullOrEmpty(line) && line[0] == '#' && line[1..] is "HELD" or "NOTHELD")
                {
                    //#HELD indicates commands that activate when keys are pressed regardless if they were pressed the
                    //previous frame (they can be held down)
                    //#NOTHELD indicates the opposites (only actives once when the keys are first presssed)
                    bool isHeld = line[1..] == "HELD";

                    if (isHeld && heldHandler is null)
                    {
                        heldHandler = CreateInputHandlerFromStream(reader, true, out line);
                    }
                    else if (isHeld && heldHandler is not null)
                    {
                        Debug.WriteLine($"{methodName} (parse warning): multiple #HELD regions. Only returning" +
                                        $"the first one.");
                    }

                    if (!isHeld && notHeldHandler is null)
                    {
                        notHeldHandler = CreateInputHandlerFromStream(reader, false, out line);
                    }
                    else if (!isHeld && notHeldHandler is not null)
                    {
                        Debug.WriteLine($"{methodName} (parse warning): multiple #NOTHELD regions. Only returning" +
                                        $"the first one.");
                    }
                }
                else
                {
                    line = reader.ReadLine();
                }
            }

            if (heldHandler is null)
            {
                Debug.WriteLine($"{methodName}: did not find #HELD REGION! Returning an empty input handler.");
            }

            if (notHeldHandler is null)
            {
                Debug.WriteLine($"{methodName}: did not find #NOTHELD region! Returning an empty input handler.");
            }

            return (notHeldHandler ?? new InputHandler(false), heldHandler ?? new InputHandler(true));
        }
        
        private InputHandler CreateInputHandlerFromStream(StreamReader reader, bool isHeld, out string? lastLine)
        {
            const string methodName = nameof(MainGame) + "." + nameof(CreateInputHandlerFromStream);

            Dictionary<string, CommandEntry> commands = new();

            //'#' indicates a region. stop searching when we hit a new region.
            string? line;
            while ((line = reader.ReadLine()) is not null && line[0] != '#')
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
                        Keys[] keys = KeyboardExtensions.ParseKeyboardState(keysStr);
                        
                        if (keys.Length > 0)
                        {
                            commands[funcName] = new CommandEntry(keys, command);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"{methodName}: cannot get function name from following line: {line}");
                    }
                }
                else
                {
                    Debug.WriteLine($"{methodName}: cannot find func name behind colon in the following line: " +
                                    $"{line}. Skipping.");
                }
            }

            lastLine = line;
            return new InputHandler(commands, isHeld);
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
                "unfocus" => new UnfocusCommand(_mainGrid),
                "engage_scene_viewer_camera_movement" => new ControlSceneCameraCommand(_mainGrid),
                "move_camera_forward" when _currentScene is not null => 
                    new MoveCameraCommand(MovementHelper.Direction.Forward, _currentScene.World),
                "move_camera_backward" when _currentScene is not null => 
                    new MoveCameraCommand(MovementHelper.Direction.Backwards, _currentScene.World),
                "move_camera_left" when _currentScene is not null => 
                    new MoveCameraCommand(MovementHelper.Direction.Left, _currentScene.World),
                "move_camera_right" when _currentScene is not null => 
                    new MoveCameraCommand(MovementHelper.Direction.Right, _currentScene.World),
                "move_camera_up" when _currentScene is not null => 
                    new MoveCameraCommand(MovementHelper.Direction.Up, _currentScene.World),
                "move_camera_down" when _currentScene is not null => 
                    new MoveCameraCommand(MovementHelper.Direction.Down, _currentScene.World),
                _ => IKeyCommand.EmptyCommand
            }) is not EmptyCommand;
    }
}