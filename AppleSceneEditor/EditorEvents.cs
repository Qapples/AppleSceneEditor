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
            if (_jsonObjectToEdit is null || _currentJsonObject is null) return;

            JsonArray? componentArray =
                _currentJsonObject.Arrays.FirstOrDefault(a => a.Name is not null && a.Name.ToLower() == "components");
            if (componentArray is null)
            {
                Debug.WriteLine($"{nameof(FinishButtonClick)}: cannot find array with name of \"components\" in " +
                                $"the current json object. Cannot finish.");
                return;
            }

            _addComponentWindow.Close();

            JsonObject newObject = new(null, _currentJsonObject);
            newObject.Properties.Add(new JsonProperty("type", typeName, newObject, JsonValueKind.String));
            
            componentArray.Add(newObject);
        }
    }
}