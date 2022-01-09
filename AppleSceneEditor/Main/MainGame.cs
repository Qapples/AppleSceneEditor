using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AppleSceneEditor.Commands;
using AppleSceneEditor.ComponentFlags;
using AppleSceneEditor.Exceptions;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Factories;
using AppleSceneEditor.Input;
using AppleSceneEditor.Input.Commands;
using AppleSceneEditor.UI;
using AppleSerialization;
using AppleSerialization.Info;
using AppleSerialization.Json;
using AssetManagementBase;
using DefaultEcs;
using DefaultEcs.System;
using FontStashSharp;
using GrappleFightNET5.Components;
using GrappleFightNET5.Resource.Info;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Properties;
using Myra.Graphics2D.UI.Styles;
using Myra.Utility;
using Environment = AppleSerialization.Environment;
using Scene = GrappleFightNET5.Runtime.Scene;

namespace AppleSceneEditor
{
    public partial class MainGame : Game
    {
#nullable disable
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Project _project;
        private Desktop _desktop;

        private Window _addComponentWindow;
        private Window _alreadyExistsWindow;
        private Window _settingsWindow;
        
        //TODO: The way we handle args right now is for sure a mess. Not super important but later down the line improve the way we do this.
        private readonly string _uiPath;
        private readonly string _stylesheetPath;
        private readonly string _defaultWorldPath;
        private readonly string _configPath;

        private Dictionary<string, JsonObject> _prototypes;
#nullable enable

        private Scene? _currentScene;

        private List<JsonObject> _jsonObjects;
        private JsonObject? _currentJsonObject;

        private ISystem<GameTime>? _drawSystems;

        private CommandStream _commands;
        private InputHandler _notHeldInputHandler;
        private InputHandler _heldInputHandler;

        private Viewport _sceneViewport;
        private Viewport _overallViewport;

        private Grid _mainGrid;
        private HorizontalMenu _mainMenu;

        private EntityViewer _entityViewer;
        private FileViewer _fileViewer;
       
        public MainGame(string[] args)
        {
            string root = Path.Combine("..", "..", "..");
            
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = Path.Combine(root, "Content");
            IsMouseVisible = true;

            _uiPath = Path.Combine(Content.RootDirectory, "Menu.xmmp");
            _stylesheetPath = Path.Combine(Content.RootDirectory, "Stylesheets", "editor_ui_skin.xmms");
            _defaultWorldPath = Path.Combine(root, "Examples", "BasicWorld", "BasicWorld.world");
            _configPath = Path.Combine(root, "Config");

            StringComparison comparison = StringComparison.Ordinal;
            foreach (string arg in args)
            {
                if (arg.IndexOf('=') < 0)
                {
                    Debug.WriteLine($"MainGame constructor: argument ({arg}) does not have an equal sign.");
                    continue;
                }

                try
                {
                    string path = Path.GetFullPath(arg[(arg.IndexOf('=') + 1)..]);

                    if (arg.StartsWith("--ui_path=", comparison)) _uiPath = path;
                    if (arg.StartsWith("--stylesheet_path=", comparison)) _stylesheetPath = path;
                    if (arg.StartsWith("--default_world=", comparison)) _defaultWorldPath = path;
                    if (arg.StartsWith("--config_path=", comparison)) _configPath = path;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"MainGame constructor: failed parsing argument: {arg}. With exception: {e}");
                }
            }

            _uiPath = Path.GetFullPath(_uiPath);
            _stylesheetPath = Path.GetFullPath(_stylesheetPath);
            _defaultWorldPath = Path.GetFullPath(_defaultWorldPath);
            _configPath = Path.GetFullPath(_configPath);

            _commands = new CommandStream();
        }
        
        protected override void Initialize()
        {            
            MyraEnvironment.Game = this;

            RawContentManager contentManager = new(GraphicsDevice, Content.RootDirectory);

            Environment.GraphicsDevice = GraphicsDevice;
            Environment.ContentManager = contentManager; 
            
            //Serialization types
            AddExternalType(typeof(MeshInfo));
            AddExternalType(typeof(TextureInfo));
            AddExternalType(typeof(ScriptInfo));
            AddExternalType(typeof(TransformInfo));
            AddExternalType(typeof(PlayerControllerInfo));
            AddExternalType(typeof(ValueInfo));
            AddExternalType(typeof(CameraInfo));

            string fontPath = Path.GetFullPath(Path.Combine(Content.RootDirectory, "Fonts", "Default"));
            Environment.DefaultFontSystem = contentManager.LoadFactory(Directory.GetFiles(fontPath),
                new FontSystem(), "Default");
            
            _jsonObjects = new List<JsonObject>();

            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.ApplyChanges();

            _overallViewport = GraphicsDevice.Viewport;
            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            _spriteBatch = new SpriteBatch(GraphicsDevice);

            //paths
            string typeAliasPath = Path.Combine(_configPath, "TypeAliases.txt");
            string keybindPath = Path.Combine(_configPath, "Keybinds.txt");
            string prototypesPath = Path.Combine(_configPath, "ComponentPrototypes.json");
            string settingsMenuPath = Path.Combine(Content.RootDirectory, "Settings.xmmp");

            //ensure that these paths exist.
            string[] missingConfigFiles = (from file in new[] {typeAliasPath, keybindPath, prototypesPath}
                where !File.Exists(file)
                select file).ToArray();

            if (missingConfigFiles.Length > 0)
            {
                throw new RequiredConfigFileNotFoundException(missingConfigFiles);
            }

            //inputhandler will be initialized later when a proper world is loaded and everything is set.
            Environment.LoadTypeAliasFileContents(File.ReadAllText(typeAliasPath));
            _prototypes = IOHelper.CreatePrototypesFromFile(prototypesPath);

            //load stylesheet
            string folder = Path.GetDirectoryName(Path.GetFullPath(_uiPath));
            PropertyGridSettings settings = new()
            {
                AssetManager = new AssetManager(new FileAssetResolver(folder)),
                BasePath = folder
            };

            Stylesheet stylesheet = _stylesheetPath is null
                ? Stylesheet.Current
                : settings.AssetManager.Load<Stylesheet>(_stylesheetPath);
            Stylesheet.Current = stylesheet;

            _openFileDialog = CreateOpenFileDialog();
            _newFileDialog = CreateNewFileDialog();

            //load the UI from the path define as _uiPath
            _project = Project.LoadFromXml(File.ReadAllText(_uiPath), settings.AssetManager, stylesheet);
            _desktop = new Desktop
            {
                Root = _project.Root
            };
            
            //create windows and dialogs
            _addComponentWindow = DialogFactory.CreateNewComponentDialog(_prototypes!.Keys, FinishButtonClick);
            _alreadyExistsWindow = DialogFactory.CreateAlreadyExistsDialog();
            _settingsWindow = DialogFactory.CreateSettingsDialogFromFile(_desktop, settingsMenuPath, _configPath);

            //handle specific widgets (adding extra functionality, etc.). if MainMenu, MainPanel, or MainGrid are not
            //found, then we can no longer continue running and we must fire an exception.
            List<string> missingWidgets = new() {"MainMenu", "MainPanel", "MainGrid", "ToolMenu"};
            
            _project.Root.ProcessWidgets(widget =>
            {
                switch (widget.Id)
                {
                    case "MainMenu":
                    {
                        if (widget is not HorizontalMenu menu) return false;
                        
                        MenuItem? fileItemOpen = menu.FindMenuItemById("MenuFileOpen");
                        MenuItem? fileItemNew = menu.FindMenuItemById("MenuFileNew");
                        MenuItem? settingsMenuOpen = menu.FindMenuItemById("SettingsMenuOpen");

                        if (fileItemOpen is not null) fileItemOpen.Selected += MenuFileOpen;
                        if (fileItemNew is not null) fileItemNew.Selected += MenuFileNew;
                        if (settingsMenuOpen is not null) settingsMenuOpen.Selected += SettingsMenuOpen;

                        _mainMenu = menu;
                        _mainMenu.AcceptsKeyboardFocus = false;

                        missingWidgets.Remove("MainMenu");

                        break;
                    }
                    case "MainPanel":
                    {
                        if (widget is not Panel panel) return false;
                        if (panel.FindWidgetById("AddComponentButton") is not TextButton addComponent ||
                            panel.FindWidgetById("AddEntityButton") is not TextButton addEntity) return false;

                        addComponent.Click += AddComponentButtonClick;
                        //addEntity.Click += AddEntityButtonClick;

                        panel.AcceptsKeyboardFocus = true;
                        missingWidgets.Remove("MainPanel");
                        
                        break;
                    }
                    case "MainGrid":
                    {
                        if (widget is not Grid grid) return false;
                        
                        _mainGrid = grid;
                        _mainGrid.AcceptsKeyboardFocus = true;
                        
                        missingWidgets.Remove("MainGrid");
                        
                        break;
                    }
                    case "ToolMenu":
                    {
                        if (widget is not StackPanel toolPanel) return false;

                        ImageButton? moveToolButton = toolPanel.FindWidgetById("MoveToolButton") as ImageButton;
                        ImageButton? rotateToolButton = toolPanel.FindWidgetById("RotateToolButton") as ImageButton;
                        ImageButton? scaleToolButton = toolPanel.FindWidgetById("ScaleToolButton") as ImageButton;

                        if (moveToolButton is null || rotateToolButton is null || scaleToolButton is null)
                        {
                            throw new WidgetIsIncorrectTypeException(typeof(ImageButton),
                                ("MoveToolButton", moveToolButton), ("RotateToolButton", rotateToolButton),
                                ("ScaleToolButton", scaleToolButton));
                        }

                        SolidBrush selectedBrush = new(Color.SkyBlue);

                        moveToolButton.Click += (o, _) =>
                        {
                            if (o is not ImageButton button) return;

                            _currentScene?.World.Set(AxisType.Move);

                            button.Background = selectedBrush;
                            rotateToolButton.Background = null;
                            scaleToolButton.Background = null;
                        };
                        
                        rotateToolButton.Click += (o, _) =>
                        {
                            if (o is not ImageButton button) return;

                            _currentScene?.World.Set(AxisType.Rotation);

                            moveToolButton.Background = null;
                            button.Background = selectedBrush;
                            scaleToolButton.Background = null;
                        };
                        
                        scaleToolButton.Click += (o, _) =>
                        {
                            if (o is not ImageButton button) return;

                            _currentScene?.World.Set(AxisType.Scale);

                            moveToolButton.Background = null;
                            rotateToolButton.Background = null;
                            button.Background = selectedBrush;
                        };

                        toolPanel.AcceptsKeyboardFocus = true;

                        missingWidgets.Remove("ToolMenu");
                        
                        break;
                    }
                }

                return true;
            });

            if (missingWidgets.Count > 0)
            { 
                throw new WidgetNotFoundException(missingWidgets.ToArray());
            }


            string fileIconsPath = Path.Combine(Content.RootDirectory, "Textures", "FileIcons");
            string examplesPath = Path.Combine(Content.RootDirectory, "..", "Examples");

            _fileViewer = new FileViewer(examplesPath, 8,
                IOHelper.GetTexturesFromDirectory(fileIconsPath, GraphicsDevice), _commands);
            _mainGrid.AddChild(new ScrollViewer {GridColumn = 1, GridRow = 1, Content = _fileViewer});

            //load the default world (if provided)
            if (_defaultWorldPath is not null)
            {
                DirectoryInfo? parentDirectory = Directory.GetParent(_defaultWorldPath);

                if (parentDirectory is null)
                {
                    Debug.WriteLine($"Cant get parent directory from path when getting _defaultWorldPath: " +
                                    $"{_defaultWorldPath}.");
                }
                else
                {
                    _currentScene = InitScene(parentDirectory.FullName);
                    
                    //init _inputHelper here since by then all the fields should have been initialized so far.
                    (_notHeldInputHandler, _heldInputHandler) =
                        CreateInputHandlersFromFile(Path.Combine(_configPath, "Keybinds.txt"));
                }
            }
        }

        protected override void UnloadContent()
        {
            long beforeMemoryCount = GC.GetTotalMemory(false);
            Debug.WriteLine($"Unloading. Data amount:       {beforeMemoryCount}");
            
            _spriteBatch.Dispose();
            _graphics.Dispose();

            _project = null;
            _desktop = null;

            _addComponentWindow = null;
            _alreadyExistsWindow = null;

            _openFileDialog = null!;
            _newFileDialog = null!;

            _project = null;
            
            _currentScene?.Dispose();

            _jsonObjects = null!;
            _currentJsonObject = null;
            
            _drawSystems?.Dispose();

            _commands.Dispose();
            _notHeldInputHandler.Dispose();
            _heldInputHandler.Dispose();

            _mainPanelHandler?.Dispose();
            _mainPanelHandler = null;
            _entityViewer = null!;
            _fileViewer = null!;

            Stylesheet.Current = null;

            Dispose();
            
            long afterMemoryCount = GC.GetTotalMemory(true);
            Debug.WriteLine($"Unload complete. Data amount: {afterMemoryCount}");
        }

        private KeyboardState _previousKbState;
        private MouseState _previousMouseState;

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(
                Keys.OemTilde)) Exit();

            KeyboardState kbState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            if (_currentScene is not null)
            {
                World world = _currentScene.World;

                IKeyCommand[] heldCommands = _heldInputHandler.GetCommands(ref kbState, ref _previousKbState);
                IKeyCommand[] notHeldCommand = _notHeldInputHandler.GetCommands(ref kbState, ref _previousKbState);

                bool cameraMovementActive = _mainGrid.IsKeyboardFocused &&
                                            GlobalFlag.IsFlagRaised(GlobalFlags.UserControllingSceneViewer);

                if (heldCommands.Length > 0)
                {
                    foreach (IKeyCommand command in heldCommands)
                    {
                        if (command is not EmptyCommand &&
                            (command is not MoveCameraCommand || cameraMovementActive))
                        {
                            command.Execute();
                        }
                    }
                }

                if (notHeldCommand.Length > 0)
                {
                    foreach (IKeyCommand command in notHeldCommand)
                    {
                        //we don't have to check for MoveCameraCommand here because it will always be a held command.
                        if (command is not EmptyCommand)
                        {
                            command.Execute();
                        }
                    }
                }

                //update camera movement rotation. Camera movement is handled via commands (handled above).
                if (cameraMovementActive)
                {
                    ref var properties = ref world.Get<CameraProperties>();
                    ref var camera = ref world.Get<Camera>();

                    properties.YawDegrees += (_previousMouseState.X - mouseState.X) / camera.Sensitivity;
                    properties.PitchDegrees += (_previousMouseState.Y - mouseState.Y) / camera.Sensitivity;
                    camera.RotateFromDegrees(properties.YawDegrees, properties.PitchDegrees);
                }

                //if the user clicks the mouse within the scene editor while not moving the camera then indicate
                //that we want to select an entity. this is likely to be temporary
                if (_mainGrid.IsKeyboardFocused && !GlobalFlag.IsFlagRaised(GlobalFlags.UserControllingSceneViewer) &&
                    mouseState.LeftButton == ButtonState.Pressed &&
                    _previousMouseState.LeftButton == ButtonState.Released)
                {
                    GlobalFlag.SetFlag(GlobalFlags.FireSceneEditorRay, true);
                }

                //account for flags

                //SelectedEntityFlag indicates that an entity needs to be selected. 
                if (GlobalFlag.IsFlagRaised(GlobalFlags.EntitySelected))
                {
                    ref var flag = ref world.Get<SelectedEntityFlag>();

                    //the selected entity should have a string id
                    SelectEntity(_currentScene, flag.SelectedEntity.Get<string>());

                    GlobalFlag.SetFlag(GlobalFlags.EntitySelected, false);
                }

                if (world.Has<EntityTransformChangedFlag>() && _currentJsonObject is not null)
                {
                    ref var flag = ref world.Get<EntityTransformChangedFlag>();

                    _currentJsonObject.UpdateTransform(flag.CurrentTransform);
                    _mainPanelHandler?.RebuildUI();

                    world.Remove<EntityTransformChangedFlag>();
                }

                if (world.Has<AddedEntityFlag>())
                {
                    ref var flag = ref world.Get<AddedEntityFlag>();

                    _jsonObjects.Add(flag.AddedJsonObject);
                    _entityViewer.CreateEntityButtonGrid(flag.AddedEntity.Get<string>(), flag.AddedEntity, out _);
                    
                    world.Remove<AddedEntityFlag>();
                }

                if (world.Has<RemovedEntityFlag>())
                {
                    ref var flag = ref world.Get<RemovedEntityFlag>();
                    string id = flag.RemovedEntityId;

                    JsonObject? foundJsonObject = _jsonObjects.FindJsonObjectById(id);
                    if (foundJsonObject is not null)
                    {
                        _jsonObjects.Remove(foundJsonObject);
                    }
                    
                    _entityViewer.RemoveEntityButtonGrid(id);
                    
                    world.Remove<RemovedEntityFlag>();
                }
            }

            _previousKbState = kbState;
            _previousMouseState = mouseState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _desktop.Render();

            //greater than 1 widget usually means a window is opened (however, the top menu will also increase the
            //(widget count)
            if (_desktop.Widgets.Count > 1 && _desktop.ContextMenu?.Visible is not true)
            {
                return;
            }

            //if scene port is uninitialized, then initialize it. (the desktop must first be rendered before the
            //grid lines are initialized)
            if (_sceneViewport.Width == 0 || _sceneViewport.Height == 0)
            {
                //there should be two x grid lines, and 1 y grid line bordering the scene viewer grid.
                //y is the height of the main menu at the top of the screen.
                var (x, x1) = (_mainGrid.GridLinesX[0], _mainGrid.GridLinesX[1]);
                var (width, height) = (x1 - x, _mainGrid.GridLinesY[0]);
                int y = _mainMenu.Bounds.Size.Y;

                _sceneViewport = new Viewport(x, y, width, height);
            }

            if (_drawSystems is not null)
            {
                //The depth buffer is changed everytime we use the spritebatch to draw 2d objects. So, we must "reset" the
                //by setting the depth stencil state to it's default value before drawing any 3d objects. If we don't do
                //this, then all the models will be drawn incorrectly.
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                
                //set the viewport so that the scene is drawn within the scene viewer
                GraphicsDevice.Viewport = _sceneViewport;
                
                _drawSystems.Update(gameTime);
                
                GlobalFlag.SetFlag(GlobalFlags.FireSceneEditorRay, false);
            }

            GraphicsDevice.Viewport = _overallViewport;
            
            base.Draw(gameTime);
        }
        
        private static void AddExternalType(Type type)
        {
            if (type.AssemblyQualifiedName is null) return;
            
            string[] typeName = type.AssemblyQualifiedName.Split(", ");

            AppleSerialization.Environment.ExternalTypes.Add($"{typeName[0]}, {typeName[1]}", type);
        }
    }
}