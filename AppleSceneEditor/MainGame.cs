using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AppleSerialization;
using AppleSerialization.Json;
using AssetManagementBase;
using FontStashSharp;
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
        
        private readonly string _uiPath;
        
        private FontSystem _currentFontSystem;
#nullable enable
        private readonly string? _stylesheetPath;
        private readonly string? _defaultWorldPath;
        private readonly string? _keybindConfigPath;
        
        private Scene? _currentScene;

        private List<JsonObject> _jsonObjects;
        private JsonObject _currentJsonObject;

        public MainGame(string[] args)
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = Path.Combine("..", "..", "..", "Content");
            IsMouseVisible = true;
            
            _uiPath = Path.Combine("..", "..", "..", "Content", "Menu.xmmp");

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
                    if (arg.StartsWith("--keybind_config_path=", comparison)) _keybindConfigPath = path;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"MainGame constructor: failed parsing argument: {arg}. With exception: {e}");
                }
            }

            _uiPath = Path.GetFullPath(_uiPath);
            if (_stylesheetPath is not null) _stylesheetPath = Path.GetFullPath(_stylesheetPath);
        }

        protected override void Initialize()
        {
            MyraEnvironment.Game = this;

            RawContentManager contentManager = new(GraphicsDevice, Content.RootDirectory);

            Environment.GraphicsDevice = GraphicsDevice;
            Environment.ContentManager = contentManager;

            string fontPath = Path.GetFullPath(Path.Combine(Content.RootDirectory, "Fonts", "Default"));
            Environment.DefaultFontSystem = contentManager.LoadFactory(Directory.GetFiles(fontPath),
                new FontSystem(), "Default");

            _jsonObjects = new List<JsonObject>();

            Config.ParseConfigFile(File.ReadAllText(Path.Combine("..", "..", "..", "Config.txt")));

            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            _spriteBatch = new SpriteBatch(GraphicsDevice);

            string folder = Path.GetDirectoryName(Path.GetFullPath(_uiPath));
            PropertyGridSettings settings = new()
            {
                AssetManager = new AssetManager(new FileAssetResolver(folder)),
                BasePath = folder
            };
            Stylesheet stylesheet = _stylesheetPath is null
                ? Stylesheet.Current
                : settings.AssetManager.Load<Stylesheet>(_stylesheetPath);

            _project = Project.LoadFromXml(File.ReadAllText(_uiPath), settings.AssetManager, stylesheet);

            _desktop = new Desktop
            {
                Root = _project.Root
            };

            _project.Root.ProcessWidgets(widget =>
            {
                if (widget.Id == "MainMenu")
                {
                    if (widget is not HorizontalMenu menu) return false;

                    MenuItem? fileItemOpen = menu.FindMenuItemById("MenuFileOpen");
                    MenuItem? fileItemNew = menu.FindMenuItemById("MenuFileNew");

                    if (fileItemOpen is not null) fileItemOpen.Selected += MenuFileOpen;
                    if (fileItemNew is not null) fileItemNew.Selected += MenuFileNew;
                }

                return true;
            });

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
                    GetJsonObjectsFromScene(parentDirectory.FullName);
                    
                    InitUIFromScene(_currentScene);
                }
            }
        }

        protected override void UnloadContent()
        {
        }
        
        
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(
                Keys.Escape)) Exit();
            
            if (_currentScene is not null)
            {
                Input.InputHelper.Update(Keyboard.GetState(), _project.Root, _currentScene,
                    new object?[] {_propertyGrid, this});
            }

            Input.InputHelper.PreviousKeyboardState = Keyboard.GetState();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _desktop.Render();
            base.Draw(gameTime);
        }
    }
}