using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppleSceneEditor.Commands;
using AppleSceneEditor.ComponentFlags;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Input;
using AppleSceneEditor.Input.Commands;
using AppleSceneEditor.Exceptions;
using AppleSceneEditor.Systems;
using AppleSceneEditor.UI;
using AppleSerialization;
using AppleSerialization.Converters;
using AppleSerialization.Json;
using DefaultEcs;
using DefaultEcs.System;
using GrappleFight.Collision;
using GrappleFight.Collision.Hulls;
using GrappleFight.Components;
using GrappleFight.Runtime;
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

        internal Scene InitScene(string sceneDirectory)
        {
            string globalContentDirectory = Path.Combine(sceneDirectory, "..", "..", "Global");
            
            // We use typeof(string) here to get an assembly to pass through. We don't really need to load scripts.
            // Might be a good idea to go back into the engine code to make it so that you don't have to pass an
            // assembly.
            Scene scene = new(sceneDirectory, globalContentDirectory, GraphicsDevice, typeof(string).Assembly,
                _serializationSettings, true);

            ComplexBox emptyRegion = new();
            scene.World.Set(new OctreeNode(null, 0, ref emptyRegion));
            
            _jsonObjects = IOHelper.CreateJsonObjectsFromScene(sceneDirectory);

            scene.Compile();

            //initialize world-wide components
            scene.World.Set(new Camera(Vector3.Zero, Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(95f),
                GraphicsDevice.DisplayMode.AspectRatio, 1f, 10000f), GraphicsDevice.Viewport, 2f, 0));
                    
            scene.World.Set(new CameraProperties
            {
                YawDegrees = 0f,
                PitchDegrees = 0f,
                CameraSpeed = 0.5f
            });
            
            scene.World.Set(AxisType.Move);

            _commands.Dispose();
            _commands = new CommandStream();

            _drawSystems?.Dispose();
            _drawSystems = new SequentialSystem<GameTime>(
                new EntityDrawSystem(scene.World, GraphicsDevice),
                new AxisDrawSystem(scene.World, GraphicsDevice, _commands),
                new SceneIconDrawSystem(scene.World, GraphicsDevice, _sceneIcons));

            _entityViewer?.Dispose();
            _entityViewer = new EntityViewer(Path.Combine(sceneDirectory, "Entities"), scene.World,
                (StackPanel) _desktop.Root.FindWidgetById("EntityStackPanel"), _jsonObjects, _commands, _desktop);
            
            _mainPanelHandler?.Dispose();
            _mainPanelHandler = null;

            _fileViewer.CurrentDirectory = sceneDirectory;
            _fileViewer.World = scene.World;
            _fileViewer.ChangeCommandStream(_commands);
            
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

            _currentScene = scene;
            
            _notHeldInputHandler?.Dispose();
            _heldInputHandler?.Dispose();
            
            string keybindPath = Path.Combine(_configPath, "Keybinds.txt");
            _notHeldInputHandler = new InputHandler(keybindPath, TryGetCommandFromFunctionName, false);
            _heldInputHandler = new InputHandler(keybindPath, TryGetCommandFromFunctionName, true);

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

            foreach (Widget widget in _desktop.Root.GetChildren())
            {
                if (widget.Id != "MainGrid") continue;

                if (widget is not Grid grid || !EntityExtensions.TryGetEntityById(scene, entityId, out _))
                {
                    continue;
                }

                StackPanel? propertyStackPanel = grid.TryFindWidgetById<StackPanel>("PropertyStackPanel");
                if (propertyStackPanel is null) continue;

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
                    continue;
                }

                _currentJsonObject = selectedJsonObject;

                if (_mainPanelHandler is null)
                {
                    try
                    {
                        _mainPanelHandler = new ComponentPanelHandler(_desktop, selectedJsonObject,
                            propertyStackPanel, _commands, _serializationSettings);
                    }
                    catch (ComponentsNotFoundException e)
                    {
                        Debug.WriteLine(e);
                        continue;
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
                        continue;
                    }
                }

                //highlight the entity in the entity viewer
                VerticalStackPanel? stackPanel =
                    grid.TryFindWidgetById<VerticalStackPanel>("EntityStackPanel");
                if (stackPanel is null)
                {
                    Debug.WriteLine("Can't find VerticalStackPanel with ID of \"EntityStackPanel\".");
                    continue;
                }

                foreach (Widget panelWidget in stackPanel.Widgets)
                {
                    if (panelWidget is not TextButton button) continue;

                    int entityIdIndex = button.Id.IndexOf('_') + 1;
                    button.Background =
                        new SolidBrush(button.Id[entityIdIndex..] == entityId ? Color.SkyBlue : Color.Gray);
                }
            }
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
                "reload" when _currentScene is not null => new ReloadCommand(this, _currentScene.World,
                    _currentScene!.ScenePath), //_currentScene should not be null. It is null checked here.
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