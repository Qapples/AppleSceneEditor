using System;
using System.Diagnostics;
using System.IO;
using DefaultEcs;
using GrappleFightNET5.Scenes;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using Myra.Graphics2D.UI.Properties;
using Myra.Utility;
using SharpGLTF.Schema2;
using Container = Myra.Graphics2D.UI.Container;
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
                
                if (_currentScene is not null) InitUIFromScene(_currentScene);
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

                if (_currentScene is not null) InitUIFromScene(_currentScene);
            };

            fileDialog.ShowModal(_desktop);
        }
    }
}