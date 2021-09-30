using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using AppleSceneEditor.Systems;
using AppleSerialization.Json;
using Myra.Graphics2D.UI.File;
using JsonProperty = AppleSerialization.Json.JsonProperty;
using Scene = GrappleFightNET5.Scenes.Scene;

namespace AppleSceneEditor
{
    public partial class MainGame
    {
        //-----------------
        // EVENT METHODS
        //-----------------
        
        public void MenuFileOpen(object? sender, EventArgs? eventArgs)
        {
            FileDialog fileDialog = new(FileDialogMode.OpenFile) {Filter = "*.world", Visible = true, Enabled = true};

            fileDialog.Closed += (o, e) =>
            {
                if (!fileDialog.Result) return;

                string filePath = fileDialog.FilePath;
                if (string.IsNullOrEmpty(filePath)) return;

                _currentScene = new Scene(Directory.GetParent(filePath)!.FullName, GraphicsDevice, null, _spriteBatch,
                    true);
                GetJsonObjectsFromScene(Directory.GetParent(filePath)!.FullName);

                if (_currentScene is not null)
                {
                    InitUIFromScene(_currentScene);
                    
                    _drawSystem?.Dispose();
                    _drawSystem = new DrawSystem(_currentScene.World, GraphicsDevice);
                }
            };

            fileDialog.ShowModal(_desktop);
        }

        public void MenuFileNew(object? sender, EventArgs? eventArgs)
        {
            FileDialog fileDialog = new(FileDialogMode.ChooseFolder) {Visible = true, Enabled = true};

            fileDialog.Closed += (o, e) =>
            {
                if (!fileDialog.Result) return;

                string folderPath = fileDialog.Folder;
                if (string.IsNullOrEmpty(folderPath)) return;

                InitNewProject(folderPath);
                GetJsonObjectsFromScene(folderPath);

                if (_currentScene is not null)
                {
                    InitUIFromScene(_currentScene);
                    
                    _drawSystem?.Dispose();
                    _drawSystem = new DrawSystem(_currentScene.World, GraphicsDevice);
                }
            };

            fileDialog.ShowModal(_desktop);
        }

        private void AddComponentButtonClick(object? sender, EventArgs? eventArgs)
        {
            if (_currentJsonObject is null) return;
            
            _addComponentWindow.ShowModal(_desktop);
        }

        private void FinishButtonClick(string typeName)
        {
            const string methodName = nameof(MainGame) + "." + nameof(FinishButtonClick) + " (EditorEvents)";
            
            if (_currentJsonObject is null) return;
            
            _addComponentWindow.Close();

            if (!NewComponentPrototypes.TryGetValue(typeName, out var prototype))
            {
                Debug.WriteLine($"{methodName}: cannot find component prototype of name {typeName}! Cannot create" +
                                "new component");
                return;
            }

            _mainPanelHandler?.Components.Add(prototype);
            _mainPanelHandler?.RebuildUI();
        }
    }
}