using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AppleSerialization.Info;
using DefaultEcs;
using GrappleFightNET5.Scenes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;

namespace AppleSceneEditor
{
    public partial class MainGame
    {
        //-----------------
        // EVENT METHODS
        //-----------------
        
        private void MenuFileOpen(object? sender, EventArgs eventArgs)
        {
            FileDialog fileDialog = new(FileDialogMode.OpenFile) {Filter = "*.world", Visible = true, Enabled = true};

            fileDialog.Closed += (o, e) =>
            {
                if (!fileDialog.Result) return;

                string filePath = fileDialog.FilePath;
                if (string.IsNullOrEmpty(filePath)) return;
        
                _currentScene = new Scene(Directory.GetParent(filePath)!.FullName, GraphicsDevice, null, _spriteBatch);
            };
        
            fileDialog.ShowModal(_desktop);
        }
        
        private void MenuFileNew(object? sender, EventArgs eventArgs)
        {
            FileDialog fileDialog = new(FileDialogMode.ChooseFolder) {Visible = true, Enabled = true};
        
            fileDialog.Closed += (o, e) =>
            {
                if (!fileDialog.Result) return;
        
                string folderPath = fileDialog.Folder;
                if (string.IsNullOrEmpty(folderPath)) return;
        
                //_currentScene = GetWorldFromFile(File.ReadAllText(folderPath), folderPath);
            };
        
            fileDialog.ShowModal(_desktop);
        }
        
        
        //--------------
        // HELPER METHODS
        //--------------

        // private World InitNewProject(string folderPath, int maxCapacity = 128)
        // {
        //     string worldPath = Path.Combine(folderPath, new DirectoryInfo(folderPath).Name + ".world");
        //     
        //
        //     File.CreateText(folderPath)
        // }
    }
}