using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AppleSceneEditor.Serialization.Info;
using AssetManagementBase;
using DefaultEcs;
using DefaultEcs.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using Myra.Graphics2D.UI.Properties;
using Myra.Graphics2D.UI.Styles;
using Myra.Utility;

namespace AppleSceneEditor
{
    public class MainGame : Game
    {
#nullable disable
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Project _project;
        private Desktop _desktop;
        
        private readonly string _uiPath;
#nullable enable
        private readonly string? _stylesheetPath;
        private World? _currentWorld;
        
        public MainGame(string[] args)
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            
            _uiPath = Path.Combine("..", "..", "..", "Content", "Menu.xmmp");

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
                    if (fileItemOpen is not null) fileItemOpen.Selected += MenuFileOpen;
                }

                return true;
            });
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

        private void MenuFileOpen(object? sender, EventArgs eventArgs)
        {
            FileDialog fileDialog = new(FileDialogMode.OpenFile) {Filter = "*.world", Visible = true, Enabled = true};

            fileDialog.Closed += (o, e) =>
            {
                if (!fileDialog.Result) return;

                string filePath = fileDialog.FilePath;
                if (string.IsNullOrEmpty(filePath)) return;

                _currentWorld = GetWorldFromFile(File.ReadAllText(filePath));
            };

            fileDialog.ShowModal(_desktop);
        }

        private World? GetWorldFromFile(string worldFileContent)
        {
            string[]? fileContents = GetEntityContents(worldFileContent, true);
            if (fileContents is null)
            {
                Debug.WriteLine("No file contents present in GetWorldFromFile. Returning null...");
                return null;
            }
            
            int maxCapacity = GetWorldMaxCapacityAmount(worldFileContent) ?? 128;
            
            WorldBuilder builder = new();
            builder.AddEntities(fileContents);

            return builder.CreateWorld(maxCapacity);
        }

        private string[]? GetEntityContents(string worldFileContent, bool showDialogOnFail = false)
        {
            MatchCollection entityMatch = Regex.Matches(worldFileContent, "p:\\s*(.+)");

            //find a valid filepath and sort them based on if loading them is successful or not.
            List<string> failedFilePaths = new(), successfulFilePaths = new();

            foreach (Match match in entityMatch)
            {
                GroupCollection groups = match.Groups;
                
                //the 2nd group should be just the path
                string trimmedFilePath = groups[1].Value.Trim();

                if (File.Exists(trimmedFilePath)) successfulFilePaths.Add(trimmedFilePath);
                else failedFilePaths.Add(trimmedFilePath);
            }

            if (failedFilePaths.Count > 0 && showDialogOnFail)
            {
                ShowEntityLoadFailureDialog(failedFilePaths);
            }

            return successfulFilePaths.Count > 0 ? successfulFilePaths.Select(File.ReadAllText).ToArray() : null;
        }

        private void ShowEntityLoadFailureDialog(IEnumerable<string> fileNames)
        {
            Window window = new() {Title = "Entity file error"};

            Label errorLabel = new()
            {
                Text = string.Join('\n', fileNames), 
                HorizontalAlignment = HorizontalAlignment.Center
            };

            window.Content = errorLabel;
            
            window.ShowModal(_desktop);
        }

        private int? GetWorldMaxCapacityAmount(string worldFileContent)
        {
            Match numberMatch = Regex.Match(worldFileContent, "(WorldMaxCapacity\\s+)(\\d+)");
            GroupCollection numberGroups = numberMatch.Groups;

            //basically just find the match where it's just the int and then return it
            int capacity = 128;
            if (numberGroups.Values.Any(e => int.TryParse(e.Value, out capacity)))
            {
                return capacity;
            }

            return null;
        }
    }
}