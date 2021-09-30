using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using AppleSceneEditor.Systems;
using AppleSerialization;
using AppleSerialization.Info;
using AppleSerialization.Json;
using AssetManagementBase;
using DefaultEcs.System;
using FontStashSharp;
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
using JsonProperty = AppleSerialization.Json.JsonProperty;
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
        
        //TODO: The way we handle args right now is for sure a mess. Not super important but later down the line improve the way we do this.
        private readonly string _uiPath;
        private readonly string _stylesheetPath;
        private readonly string _defaultWorldPath;
        private readonly string _keybindConfigPath;
        private readonly string _typeAliasesConfigPath;
#nullable enable

        private Scene? _currentScene;

        private List<JsonObject> _jsonObjects;
        private JsonObject? _currentJsonObject;

        private ISystem<GameTime>? _drawSystem;

        public static Dictionary<string, JsonObject> NewComponentPrototypes { get; set; }

        private static void InitComponentPrototypes()
        {
            NewComponentPrototypes = new Dictionary<string, JsonObject>();

            /* TODO: 
             * Find a more scalable way of doing this lol. Not a huge deal but could cause issues in the future.
             * Don't wanna rely on tests and this single static method might add up a bunch in the future.
             */
            JsonObject meshInfoRoot = new("MeshInfo");
            NewComponentPrototypes.Add("MeshInfo", meshInfoRoot);

            meshInfoRoot.Properties.Add(new JsonProperty("$type", "MeshInfo", meshInfoRoot, JsonValueKind.String));
            meshInfoRoot.Properties.Add(new JsonProperty("meshIndex", 0, meshInfoRoot, JsonValueKind.Number));
            meshInfoRoot.Properties.Add(new JsonProperty("skinIndex", 0, meshInfoRoot, JsonValueKind.Number));

            meshInfoRoot.Children.Add(new JsonObject("meshPath", meshInfoRoot, new List<JsonProperty>
            {
                new("path", "", meshInfoRoot, JsonValueKind.String),
                new("isContentPath", false, meshInfoRoot, JsonValueKind.False)
            }));

            //TextureInfo
            JsonObject textureInfoRoot = new("TextureInfo");
            NewComponentPrototypes.Add("TextureInfo", textureInfoRoot);
            
            textureInfoRoot.Properties.Add(new JsonProperty("$type", "TextureInfo", meshInfoRoot,
                JsonValueKind.String));

            textureInfoRoot.Children.Add(new JsonObject("texturePath", meshInfoRoot, new List<JsonProperty>
            {
                new("path", "", meshInfoRoot, JsonValueKind.String),
                new("isContentPath", false, meshInfoRoot, JsonValueKind.False)
            }));

            //ValueInfo
            JsonObject valueInfoRoot = new("ValueInfo");
            NewComponentPrototypes.Add("ValueInfo", valueInfoRoot);

            valueInfoRoot.Properties.Add(new JsonProperty("$type", "ValueInfo", meshInfoRoot, JsonValueKind.String));
            valueInfoRoot.Properties.Add(new JsonProperty("valueType", "System.Int32", meshInfoRoot,
                JsonValueKind.String));
            valueInfoRoot.Properties.Add(new JsonProperty("value", "2", meshInfoRoot, JsonValueKind.String));
        }

        public MainGame(string[] args)
        {
            string root = Path.Combine("..", "..", "..");
            
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = Path.Combine(root, "Content");
            IsMouseVisible = true;

            _uiPath = Path.Combine(Content.RootDirectory, "Menu.xmmp");
            _stylesheetPath = Path.Combine(Content.RootDirectory, "Stylesheets", "editor_ui_skin.xmms");
            _defaultWorldPath = Path.Combine(root, "Examples", "BasicWorld", "BasicWorld.world");
            _keybindConfigPath = Path.Combine(root, "Config", "Keybinds.txt");
            _typeAliasesConfigPath = Path.Combine(root, "Config", "TypeAliases.txt");

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
            _stylesheetPath = Path.GetFullPath(_stylesheetPath);
            _defaultWorldPath = Path.GetFullPath(_defaultWorldPath);
            _keybindConfigPath = Path.GetFullPath(_keybindConfigPath);
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
            Environment.ExternalTypes.Add($"{appleInfoNamespace}.ValueInfo, {appleInfoNamespace}", typeof(ValueInfo));

            Environment.LoadTypeAliasFileContents(File.ReadAllText(_typeAliasesConfigPath));
            
            InitComponentPrototypes();

            string fontPath = Path.GetFullPath(Path.Combine(Content.RootDirectory, "Fonts", "Default"));
            Environment.DefaultFontSystem = contentManager.LoadFactory(Directory.GetFiles(fontPath),
                new FontSystem(), "Default");
            
            _jsonObjects = new List<JsonObject>();

            Config.ParseKeybindConfigFile(File.ReadAllText(_keybindConfigPath));

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
            Stylesheet.Current = stylesheet;

            _addComponentWindow = CreateNewComponentDialog();

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

                if (widget.Id == "MainPanel")
                {
                    if (widget is not Panel panel) return false;
                    if (panel.FindWidgetById("AddComponentButton") is not TextButton addButton) return false;

                    addButton.Click += AddComponentButtonClick;
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
                    
                    _drawSystem?.Dispose();
                    _drawSystem = new DrawSystem(_currentScene.World, GraphicsDevice);
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
                    new object?[] {_mainPanelHandler, this});
            }

            Input.InputHelper.PreviousKeyboardState = Keyboard.GetState();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            
            //The depth buffer is changed everytime we use the spritebatch to draw 2d objects. So, we must "reset" the
            //by setting the depth stencil state to it's default value before drawing any 3d objects. If we don't do
            //this, then all the models will be drawn incorrectly.

            if (_drawSystem is not null)
            {
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                _drawSystem.Update(gameTime);
            }

            _desktop.Render();
            base.Draw(gameTime);
        }
    }
}