using System;
using System.Diagnostics;
using System.IO;
using AssetManagementBase;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Properties;
using Myra.Graphics2D.UI.Styles;

namespace AppleSceneEditor
{
    public class MainGame : Game
    {
#nullable disable
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private Project _project;
        private Desktop _desktop;
        
        private readonly string _uiPath;
#nullable enable
        private readonly string? _stylesheetPath;
        
        public MainGame(string[] args)
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            
            _uiPath = Path.Combine("..", "..", "..", "UI", "Menu.xmmp");

            foreach (string arg in args)
            {
                if (arg.StartsWith("--ui_path=", StringComparison.Ordinal))
                {
                    _uiPath = arg[(arg.IndexOf('=') + 1)..];
                }

                if (arg.StartsWith("--stylesheet_path=", StringComparison.Ordinal))
                {
                    _stylesheetPath = arg[(arg.IndexOf('=') + 1)..];
                }
            }

            _uiPath = Path.GetFullPath(_uiPath);
            if (_stylesheetPath is not null) _stylesheetPath = Path.GetFullPath(_stylesheetPath);
        }

        protected override void Initialize()
        {
            MyraEnvironment.Game = this;
            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            spriteBatch = new SpriteBatch(GraphicsDevice);

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
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(
                Keys.Escape)) Exit();
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