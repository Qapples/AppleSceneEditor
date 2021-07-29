using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using AppleSerialization.Info;
using DefaultEcs;
using GrappleFightNET5.Scenes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using Myra.Utility;

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
                
                if (_currentScene is not null) InitUIFromScene(_currentScene);
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
        
                InitNewProject(folderPath);
                
                if (_currentScene is not null) InitUIFromScene(_currentScene);
            };
        
            fileDialog.ShowModal(_desktop);
        }
        
        
        //-----------------------
        // ADDITIONAL METHODS
        //-----------------------

        private const string BaseEntityContents = @"{
    ""components"": [
        {
        }
    ],
    ""id"" : ""Base""
}";
        
        private void InitNewProject(string folderPath, int maxCapacity = 128)
        {
            string worldPath = Path.Combine(folderPath, new DirectoryInfo(folderPath).Name + ".world");

            //create paths
            string entitiesPath = Path.Combine(folderPath, "Entities");
            Directory.CreateDirectory(Path.Combine(folderPath, "Systems"));
            Directory.CreateDirectory(Path.Combine(folderPath, "Entities"));
            Directory.CreateDirectory(Path.Combine(folderPath, "Content"));
            
            //create world file
            using (StreamWriter writer = File.CreateText(worldPath))
            {
                writer.WriteLine("WorldMaxCapacity " + maxCapacity);
                writer.Flush();
            }
            
            //add base entity
            using (StreamWriter writer = File.CreateText(Path.Combine(entitiesPath, "BaseEntity")))
            {
                writer.WriteLine(BaseEntityContents);
                writer.Flush();
            }
            

            _currentScene = new Scene(folderPath, GraphicsDevice, null, _spriteBatch);
        }

        private void InitUIFromScene(Scene scene)
        {
            //there should be a stack panel with an id of "EntityStackPanel" that should contain the entities. if it
            //exists and is a valid VerticalStackPanel, add the entities to the stack panel as buttons with their ID.
            _desktop.Root.ProcessWidgets(widget =>
            {
                if (widget.Id == "MainGrid")
                {
                    if (widget is not Grid grid) return false;

                    VerticalStackPanel? stackPanel;

                    try
                    {
                        stackPanel = grid.FindWidgetById("EntityStackPanel") as VerticalStackPanel;

                        if (stackPanel is null)
                        {
                            Debug.WriteLine("EntityStackPanel cannot be casted into an instance of VerticalStackPanel");
                            return false;
                        }
                    }
                    catch
                    {
                        Debug.WriteLine("VerticalStackPanel of ID \"EntityStackPanel\" cannot be found.");
                        return false;
                    }

                    foreach (Entity entity in scene.Entities.GetEntities())
                    {
                        if (!entity.Has<string>()) continue;
                        
                        ref var id = ref entity.Get<string>();
                        stackPanel.Widgets.Add(new TextButton
                        {
                            Text = id, Id = "EntityButton_" + id
                        });
                    }
                }

                return true;
            });
        }
    }
}