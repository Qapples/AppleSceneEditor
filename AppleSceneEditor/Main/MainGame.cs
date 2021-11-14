using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AppleSceneEditor.Commands;
using AppleSceneEditor.Exceptions;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Factories;
using AppleSceneEditor.Input;
using AppleSceneEditor.Input.Commands;
using AppleSceneEditor.Systems;
using AppleSerialization;
using AppleSerialization.Info;
using AppleSerialization.Json;
using AssetManagementBase;
using DefaultEcs.System;
using FontStashSharp;
using GrappleFightNET5.Components;
using GrappleFightNET5.Components.Camera;
using GrappleFightNET5.Scenes.Info;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Properties;
using Myra.Graphics2D.UI.Styles;
using Myra.Utility;
using Environment = AppleSerialization.Environment;
using Scene = GrappleFightNET5.Scenes.Scene;

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

        private ISystem<GameTime>? _drawSystem;

        private CommandStream _commands;
        private InputHandler _notHeldInputHandler;
        private InputHandler _heldInputHandler;

        private Viewport _sceneViewport;
        private Viewport _overallViewport;

        private Grid _mainGrid;
        private HorizontalMenu _mainMenu;
        
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
            const string sceneNamespace = nameof(GrappleFightNET5) + "." + nameof(GrappleFightNET5.Scenes);
            const string appleInfoNamespace = nameof(AppleSerialization) + "." + nameof(AppleSerialization.Info);
                            
            MyraEnvironment.Game = this;

            RawContentManager contentManager = new(GraphicsDevice, Content.RootDirectory);

            Environment.GraphicsDevice = GraphicsDevice;
            Environment.ContentManager = contentManager; 
            
            Environment.ExternalTypes.Add($"{sceneNamespace}.Info.MeshInfo, {sceneNamespace}", typeof(MeshInfo));
            Environment.ExternalTypes.Add($"{sceneNamespace}.Info.TextureInfo, {sceneNamespace}", typeof(TextureInfo));
            Environment.ExternalTypes.Add($"{sceneNamespace}.Info.ScriptInfo, {sceneNamespace}", typeof(ScriptInfo));
            Environment.ExternalTypes.Add($"{sceneNamespace}.Info.TransformInfo, {sceneNamespace}", typeof(TransformInfo));
            Environment.ExternalTypes.Add($"{appleInfoNamespace}.ValueInfo, {appleInfoNamespace}", typeof(ValueInfo));

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

            //load config
            string typeAliasPath = Path.Combine(_configPath, "TypeAliases.txt");
            string keybindPath = Path.Combine(_configPath, "Keybinds.txt");
            string prototypesPath = Path.Combine(_configPath, "ComponentPrototypes.json");

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
            Config.ParseKeybindConfigFile(File.ReadAllText(keybindPath));
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

            //create dialogs
            _addComponentWindow = DialogFactory.CreateNewComponentDialog(_prototypes!.Keys, FinishButtonClick);
            _alreadyExistsWindow = DialogFactory.CreateAlreadyExistsDialog();
            _openFileDialog = CreateOpenFileDialog();
            _newFileDialog = CreateNewFileDialog();

            //load the UI from the path define as _uiPath
            _project = Project.LoadFromXml(File.ReadAllText(_uiPath), settings.AssetManager, stylesheet);
            _desktop = new Desktop
            {
                Root = _project.Root
            };

            //handle specific widgets (adding extra functionality, etc.). if MainMenu, MainPanel, or MainGrid are not
            //found, then we can no longer continue running and we must fire an exception.
            List<string> missingWidgets = new() {"MainMenu", "MainPanel", "MainGrid"};
            
            _project.Root.ProcessWidgets(widget =>
            {
                switch (widget.Id)
                {
                    case "MainMenu":
                    {
                        if (widget is not HorizontalMenu menu) return false;
                        
                        MenuItem? fileItemOpen = menu.FindMenuItemById("MenuFileOpen");
                        MenuItem? fileItemNew = menu.FindMenuItemById("MenuFileNew");

                        if (fileItemOpen is not null) fileItemOpen.Selected += MenuFileOpen;
                        if (fileItemNew is not null) fileItemNew.Selected += MenuFileNew;

                        _mainMenu = menu;
                        missingWidgets.Remove("MainMenu");

                        break;
                    }
                    case "MainPanel":
                    {
                        if (widget is not Panel panel) return false;
                        if (panel.FindWidgetById("AddComponentButton") is not TextButton addButton) return false;

                        addButton.Click += AddComponentButtonClick;

                        missingWidgets.Remove("MainPanel");
                        
                        break;
                    }
                    case "MainGrid":
                    {
                        if (widget is not Grid grid) return false;

                        _mainGrid = grid;
                        missingWidgets.Remove("MainGrid");
                        
                        break;
                    }
                }

                return true;
            });

            if (missingWidgets.Count > 0)
            { 
                throw new WidgetNotFoundException(missingWidgets.ToArray());
            }

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
                    _currentScene = new Scene(parentDirectory.FullName, GraphicsDevice, null,
                        _spriteBatch, true);

                    _jsonObjects = IOHelper.CreateJsonObjectsFromScene(parentDirectory.FullName);
                    InitUIFromScene(_currentScene);
                    
                    _currentScene.Compile();
                    
                    _currentScene.World.Set(new Camera
                    {
                        Position = Vector3.Zero,
                        LookAt = new Vector3(0f, 0f, 20f),
                        ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(95f),
                            GraphicsDevice.DisplayMode.AspectRatio, 1f, 1000f),
                        Sensitivity = 2f
                    });
                    _currentScene.World.Set(new CameraProperties
                    {
                        YawDegrees = 0f,
                        PitchDegrees = 0f,
                        CameraSpeed = 0.5f
                    });

                    _drawSystem?.Dispose();
                    _drawSystem = new DrawSystem(_currentScene.World, GraphicsDevice);
                }
            }
        }

        protected override void UnloadContent()
        {
            long beforeMemoryCount = GC.GetTotalMemory(false);
            Debug.WriteLine($"Unloading. Data amount: {beforeMemoryCount}");
            
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
            
            _drawSystem?.Dispose();

            _commands.Dispose();

            _mainPanelHandler = null;

            Dispose();
            
            long afterMemoryCount = GC.GetTotalMemory(true);
            Debug.WriteLine($"Unload complete. Data amount: {afterMemoryCount}");
        }

        private KeyboardState _previousKbState;

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(
                Keys.Escape)) Exit();

            KeyboardState kbState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            if (_currentScene is not null)
            {
                IKeyCommand[] commands = _heldInputHandler.GetCommands(ref kbState, ref _previousKbState);
                foreach (IKeyCommand command in commands)
                {
                    if (command is not EmptyCommand)
                    {
                        command.Execute();
                    }
                }

                UpdateCamera(mouseState);
            }

            _previousKbState = kbState;
            _previousMouseState = mouseState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _desktop.Render();

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

            if (_drawSystem is not null)
            {
                //The depth buffer is changed everytime we use the spritebatch to draw 2d objects. So, we must "reset" the
                //by setting the depth stencil state to it's default value before drawing any 3d objects. If we don't do
                //this, then all the models will be drawn incorrectly.
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                
                //set the viewport so that the scene is drawn within the scene viewer
                GraphicsDevice.Viewport = _sceneViewport;
                
                _drawSystem.Update(gameTime);
            }

            GraphicsDevice.Viewport = _overallViewport;

            base.Draw(gameTime);
        }
    }
}