using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AppleSceneEditor.Commands;
using AppleSceneEditor.ComponentFlags;
using AppleSceneEditor.Exceptions;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Factories;
using AppleSceneEditor.Input;
using AppleSceneEditor.Input.Commands;
using AppleSceneEditor.UI;
using AppleSerialization;
using AppleSerialization.Converters;
using AppleSerialization.Info;
using AppleSerialization.Json;
using AssetManagementBase;
using DefaultEcs;
using DefaultEcs.System;
using FontStashSharp;
using GrappleFight.Components;
using GrappleFight.Resource;
using GrappleFight.Resource.Info;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoSound;
using Myra;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Properties;
using Myra.Graphics2D.UI.Styles;
using Scene = GrappleFight.Runtime.Scene;

namespace AppleSceneEditor
{
    public partial class MainGame : Game
    {
#nullable disable
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SerializationSettings _serializationSettings;

        private Project _project;
        private Desktop _desktop;

        private Window _addComponentWindow;
        private Window _alreadyExistsWindow;
        private Window _settingsWindow;
        private Window _hitboxEditorWindow;
        
        //TODO: The way we handle args right now is for sure a mess. Not super important but later down the line improve the way we do this.
        private readonly string _uiPath;
        private readonly string _stylesheetPath;
        private readonly string _defaultWorldPath;
        private readonly string _configPath;

        private Dictionary<string, JsonObject> _prototypes;

        private Dictionary<string, Texture2D> _sceneIcons;
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
        private HitboxEditor _hitboxEditor;
       
        public MainGame(string[] args)
        {
            string root = Path.Combine("..", "..", "..");
            
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = Path.Combine(root, "Content");
            IsMouseVisible = true;

            _uiPath = Path.Combine(Content.RootDirectory, "Menu.xmmp");
            _stylesheetPath = Path.Combine("Stylesheets", "editor_ui_skin.xmms");
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

            // _uiPath = Path.GetFullPath(_uiPath);
            // _stylesheetPath = Path.GetFullPath(_stylesheetPath);
            // _defaultWorldPath = Path.GetFullPath(_defaultWorldPath);
            // _configPath = Path.GetFullPath(_configPath);

            _commands = new CommandStream();
        }
        
        protected override void Initialize()
        {            
            MyraEnvironment.Game = this;

            string fontPath = Path.GetFullPath(Path.Combine(Content.RootDirectory, "Fonts", "Default"));

            FontSystem defaultFontSystem = new();
            foreach (string fontPaths in Directory.GetFiles(fontPath))
            {
                defaultFontSystem.AddFont(File.ReadAllBytes(fontPaths));
            }
            
            JsonConverter[] converters =
            {
                new ColorJsonConverter(),
                new RectangleJsonConverter(),
                new Vector2JsonConverter(),
                new Vector3JsonConverter(),
                new EnumJsonConverter()
            };

            _serializationSettings = new SerializationSettings(converters, Content.RootDirectory,
                new Dictionary<string, Type>(), new Dictionary<string, string>());
            
            _serializationSettings.AddConverter(new Texture2DJsonConverter(GraphicsDevice, _serializationSettings));
            _serializationSettings.AddConverter(new FontSystemJsonConverter(_serializationSettings, defaultFontSystem));
            
            //Serialization types
            AddExternalType(typeof(MeshInfo), _serializationSettings);
            AddExternalType(typeof(TextureInfo), _serializationSettings);
            AddExternalType(typeof(ScriptInfo), _serializationSettings);
            AddExternalType(typeof(SingleScriptInfo), _serializationSettings);
            AddExternalType(typeof(TransformInfo), _serializationSettings);
            AddExternalType(typeof(PlayerPropertiesInfo), _serializationSettings);
            AddExternalType(typeof(ValueInfo), _serializationSettings);
            AddExternalType(typeof(CameraInfo), _serializationSettings);
            AddExternalType(typeof(CollisionHullCollectionInfo), _serializationSettings);
            AddExternalType(typeof(SingleHullInfo), _serializationSettings);
            AddExternalType(typeof(HitboxInfo), _serializationSettings);
            AddExternalType(typeof(ContentPath), _serializationSettings);
            AddExternalType(typeof(ParentInfo), _serializationSettings);
            AddExternalType(typeof(ComponentReferenceInfo), _serializationSettings);
            AddExternalType(typeof(AudioListenerInfo), _serializationSettings);
            AddExternalType(typeof(DrawOrderInfo), _serializationSettings);
            AddExternalType(typeof(Vector2), _serializationSettings);
            AddExternalType(typeof(Color), _serializationSettings);
            
            EntityExtensions.SerializationSettings = _serializationSettings;

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

            MonoSoundLibrary.Init();
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            //paths
            string typeAliasPath = Path.Combine(_configPath, "TypeAliases.txt");
            string keybindPath = Path.Combine(_configPath, "Keybinds.txt");
            string prototypesPath = Path.Combine(_configPath, "ComponentPrototypes.json");
            string settingsMenuPath = Path.Combine(Content.RootDirectory, "Settings.xmmp");
            
            LoadTypeAliasFileContents(File.ReadAllText(typeAliasPath), _serializationSettings);
            
            //ensure that these paths exist.
            string[] missingConfigFiles = (from file in new[] {typeAliasPath, keybindPath, prototypesPath}
                where !File.Exists(file)
                select file).ToArray();

            if (missingConfigFiles.Length > 0)
            {
                throw new RequiredConfigFileNotFoundException(missingConfigFiles);
            }

            //inputhandler will be initialized later when a proper world is loaded and everything is set.
            _prototypes = IOHelper.CreatePrototypesFromFile(prototypesPath);

            //load stylesheet
            string folder = Path.GetDirectoryName(Path.GetFullPath(_uiPath));
            PropertyGridSettings settings = new()
            {
                AssetManager = AssetManager.CreateFileAssetManager(folder),
                BasePath = folder
            };

            Stylesheet stylesheet = _stylesheetPath is null
                ? Stylesheet.Current
                : settings.AssetManager.LoadStylesheet(_stylesheetPath);
            stylesheet = Stylesheet.Current;
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

            const int hitboxEditorWidth = 600;
            const int hitboxEditorHeight = 500;
            _hitboxEditorWindow = new Window
            {
                Content = new HitboxEditor(GraphicsDevice, keybindPath)
                    { Width = hitboxEditorWidth, Height = hitboxEditorHeight },
                Width = hitboxEditorWidth,
                Height = hitboxEditorHeight + 25,
                MaxWidth = hitboxEditorWidth,
                MaxHeight = hitboxEditorHeight + 25
            };

            _hitboxEditor = (HitboxEditor) _hitboxEditorWindow.Content;

            _hitboxEditorWindow.Closed += (_, _) =>
            {
                _hitboxEditor.Visible = false;
            };

            Panel mainPanel = (Panel) _project.Root;
            mainPanel.AcceptsKeyboardFocus = true;

            if (mainPanel.FindWidgetById("AddComponentButton") is TextButton addComponent &&
                mainPanel.FindWidgetById("AddEntityButton") is TextButton addEntity)
            {
                addComponent.Click += AddComponentButtonClick;
                //addEntity.Click += AddEntityButtonClick;
            }

            //handle specific widgets (adding extra functionality, etc.). if MainMenu, MainPanel, or MainGrid are not
            //found, then we can no longer continue running and we must fire an exception.
            List<string> missingWidgets = new() {"MainMenu", "MainGrid", "ToolMenu"};

            foreach (Widget widget in mainPanel.GetChildren())
            {
                switch (widget.Id)
                {
                    case "MainMenu":
                    {
                        if (widget is not HorizontalMenu menu) continue;
                        
                        MenuItem? fileItemOpen = menu.FindMenuItemById("MenuFileOpen");
                        MenuItem? fileItemNew = menu.FindMenuItemById("MenuFileNew");
                        MenuItem? settingsMenuOpen = menu.FindMenuItemById("SettingsMenuOpen");
                        MenuItem? hitboxEditorOpen = menu.FindMenuItemById("HitboxEditorOpen");

                        if (fileItemOpen is not null) fileItemOpen.Selected += MenuFileOpen;
                        if (fileItemNew is not null) fileItemNew.Selected += MenuFileNew;
                        if (settingsMenuOpen is not null) settingsMenuOpen.Selected += SettingsMenuOpen;
                        if (hitboxEditorOpen is not null) hitboxEditorOpen.Selected += HitboxEditorOpen;
                        
                        _mainMenu = menu;
                        _mainMenu.AcceptsKeyboardFocus = false;

                        missingWidgets.Remove("MainMenu");

                        break;
                    }
                    case "MainGrid":
                    {
                        if (widget is not Grid grid) continue;
                        
                        _mainGrid = grid;
                        _mainGrid.AcceptsKeyboardFocus = true;
                        
                        missingWidgets.Remove("MainGrid");
                        
                        break;
                    }
                    case "ToolMenu":
                    {
                        if (widget is not StackPanel toolPanel) continue;

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
            }

            if (missingWidgets.Count > 0)
            { 
                throw new WidgetNotFoundException(missingWidgets.ToArray());
            }
            
            string fileIconsPath = Path.Combine(Content.RootDirectory, "Textures", "FileIcons");
            string sceneIconsPath = Path.Combine(Content.RootDirectory, "Textures", "SceneIcons");
            string examplesPath = Path.Combine(Content.RootDirectory, "..", "Examples");

            _fileViewer = new FileViewer(examplesPath, 8,
                IOHelper.GetTexturesFromDirectory(fileIconsPath, GraphicsDevice), _commands);
            _mainGrid.AddChild(new ScrollViewer {GridColumn = 1, GridRow = 1, Content = _fileViewer});

            _sceneIcons = IOHelper.GetTexturesFromDirectory(sceneIconsPath, GraphicsDevice);

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
                }
            }
        }

        protected override void UnloadContent()
        {
            long beforeMemoryCount = GC.GetTotalMemory(false);
            Debug.WriteLine($"Unloading. Data amount:       {beforeMemoryCount} bytes. " +
                            $"({beforeMemoryCount / 1000000f} megabytes)");

            _spriteBatch.Dispose();
            _graphics.Dispose();

            _project = null;
            _desktop = null;

            _addComponentWindow = null;
            _alreadyExistsWindow = null;
            _settingsWindow = null;
            _hitboxEditorWindow = null;

            _hitboxEditor.Dispose();
            _hitboxEditor = null!;

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
            Debug.WriteLine($"Unload complete. Data amount: {afterMemoryCount} bytes. " +
                            $"({afterMemoryCount / 1000000f} megabytes)");
        }

        private KeyboardState _previousKbState;
        private MouseState _previousMouseState;

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.OemTilde)) Exit();

            KeyboardState kbState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            _hitboxEditor.UpdateCamera(ref kbState, ref _previousKbState, ref mouseState, ref _previousMouseState);
            _hitboxEditor.UpdateHitboxPlayback(gameTime.ElapsedGameTime);

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
            
            _hitboxEditor.Draw();

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

                _sceneViewport = new Viewport(x, y, width, height - 15);
            }

            if (_drawSystems is not null)
            {
                //The depth buffer is changed everytime we use the spritebatch to draw 2d objects. So, we must "reset" the
                //by setting the depth stencil state to it's default value before drawing any 3d objects. If we don't do
                //this, then all the models will be drawn incorrectly.
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                
                //set the viewport so that the scene is drawn within the scene viewer
                GraphicsDevice.Viewport = _sceneViewport;
                GraphicsDevice.Clear(Color.CornflowerBlue);
                
                _drawSystems.Update(gameTime);
                
                GlobalFlag.SetFlag(GlobalFlags.FireSceneEditorRay, false);
            }

            GraphicsDevice.Viewport = _overallViewport;

            base.Draw(gameTime);
        }
        
        private static void AddExternalType(Type type, SerializationSettings settings)
        {
            if (type.AssemblyQualifiedName is null) return;
            
            string[] typeName = type.AssemblyQualifiedName.Split(", ");

            settings.ExternalTypes.Add($"{typeName[0]}, {typeName[1]}", type);
        }
        
        public static void LoadTypeAliasFileContents(string fileContents, SerializationSettings settings)
        {
            foreach (Match match in Regex.Matches(fileContents, @"(\w+)\W+""([\w., ]+)"))
            {
                if (match.Groups.Count < 3) continue;

                settings.TypeAliases.Add(match.Groups[1].Value, match.Groups[2].Value);
            }
        }
    }
}