using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AppleSceneEditor.Commands;
using AppleSceneEditor.Systems;
using Myra.Graphics2D.UI.File;
using Scene = GrappleFightNET5.Scenes.Scene;

namespace AppleSceneEditor
{
    public partial class MainGame
    {
        //-----------------
        // EVENT METHODS
        //-----------------

        private FileDialog _openFileDialog;
        private FileDialog _newFileDialog;

        public void MenuFileOpen(object? sender, EventArgs? eventArgs)
        {
            _openFileDialog.ShowModal(_desktop);
        }

        public void MenuFileNew(object? sender, EventArgs? eventArgs)
        {
            _newFileDialog.ShowModal(_desktop);
        }

        public void SettingsMenuOpen(object? sender, EventArgs? eventArgs)
        {
            _settingsWindow.ShowModal(_desktop);
        }

        private void AddComponentButtonClick(object? sender, EventArgs? eventArgs)
        {
            if (_currentJsonObject is null) return;
            
            _addComponentWindow.ShowModal(_desktop);
        }

        private void AddEntityButtonClick(object? sender, EventArgs? eventArgs)
        {
            if (_currentScene is null) return;
            
            _addEntityWindow.ShowModal(_desktop);
        }

        private void FinishButtonClick(string typeName)
        {
            const string methodName = nameof(MainGame) + "." + nameof(FinishButtonClick) + " (EditorEvents)";

            _addComponentWindow.Close();

            if (_currentJsonObject is null || _mainPanelHandler is null) return;

            if (!_prototypes.TryGetValue(typeName, out var prototype))
            {
                Debug.WriteLine($"{methodName}: cannot find component prototype of name {typeName}! Cannot create" +
                                "new component");
                return;
            }

            if (_mainPanelHandler.Components.Any(e => e.FindProperty("$type")?.Value as string == typeName))
            {
                Debug.WriteLine($"{methodName}: component of type {typeName} already exists in the currently " +
                                $"selected entity!");

                _alreadyExistsWindow.ShowModal(_desktop);

                return;
            }
            
            _commands.AddCommandAndExecute(new AddComponentCommand(prototype, _mainPanelHandler));
        }

        private void NewEntityOkClick(string entityPath)
        {
#if DEBUG
            const string methodName = nameof(NewEntityOkClick) + "(event)";      
#endif
            string? entityDirectory = Path.GetDirectoryName(entityPath);
            if (entityDirectory is null || !Directory.Exists(entityDirectory))
            {
                Debug.WriteLine($"{methodName}: failed to get directory of entity.");
                return;
            }
            
            //Directory.GetFiles returns the full path, not just the name of the files.
            if (Directory.GetFiles(entityDirectory).Any(s => Path.GetFullPath(s) == Path.GetFullPath(entityPath)))
            {
                Debug.WriteLine(
                    $"{methodName}: the entity file {entityPath} already exists!");
                return;   
            }
            

            string id = Path.GetFileNameWithoutExtension(entityPath);
            _commands.AddCommandAndExecute(new AddEntityCommand(entityPath, GenerateBlankEntityFile(id),
                _currentScene.World, _entityViewerStackPanel, _jsonObjects));
        }
        
        private static string GenerateBlankEntityFile(string entityId) => $@"{{
    ""components"": [
        {{
            ""$type"": ""TransformInfo"",
            ""position"": ""0 0 0"",
            ""scale"": ""1 1 1"",
            ""rotation"": ""0 0 0"",
            ""velocity"": ""0 0 0""
        }}
    ],
    ""id"" : ""{entityId}""
}}";
    }
}