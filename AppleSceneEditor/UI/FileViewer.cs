using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using AppleSceneEditor.Commands;
using AppleSceneEditor.Extensions;
using DefaultEcs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace AppleSceneEditor.UI
{
    public sealed class FileViewer : Grid
    {
        private string _currentDirectory;

        public string CurrentDirectory
        {
            get => _currentDirectory;
            set
            {
                _currentDirectory = value;
                BuildUI();
            }
        }
        
        public int ItemsPerRow { get; set; }
        
        public World? World { get; set; }

        private readonly Dictionary<string, Texture2D> _fileIcons;
        
        private CommandStream _globalCommands;

        private bool _isRightClick;

        private VerticalMenu _fileContextMenu;
        private VerticalMenu _folderContextMenu;

        private string _selectedItemName;

        public FileViewer(TreeStyle? style, string directory, int itemsPerRow, World? world,
            Dictionary<string, Texture2D> fileIcons, CommandStream globalCommands)
        {
            (_currentDirectory, ItemsPerRow, World, _fileIcons, _globalCommands) =
                (directory, itemsPerRow, world, fileIcons, globalCommands);

            ColumnSpacing = 4;
            RowSpacing = 4;

            if (style is not null)
            {
                ApplyWidgetStyle(style);
            }

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            
            for (int i = 0; i < ItemsPerRow; i++)
            {
                //myra is wack. Proportions are over 3 instead of over 1.
                ColumnsProportions.Add(new Proportion(ProportionType.Part, 3f / ItemsPerRow));
            }

            _fileContextMenu = CreateFileContextMenu();
            _folderContextMenu = CreateFolderContextMenu();

            _selectedItemName = "";

            BuildUI();
        }

        public FileViewer(string directory, int itemsPerRow, World? world, Dictionary<string, Texture2D> fileIcons,
            CommandStream globalCommands) : this(Stylesheet.Current.TreeStyle, directory, itemsPerRow, world, fileIcons,
            globalCommands)
        {
        }
        
        public FileViewer(string directory, int itemsPerRow, Dictionary<string, Texture2D> fileIcons,
            CommandStream globalCommands) : this(Stylesheet.Current.TreeStyle, directory, itemsPerRow, null, fileIcons,
            globalCommands)
        {
        }

        public void ChangeCommandStream(CommandStream newStream) => _globalCommands = newStream;

        private const string FolderIconName = "folder_icon";

        private VerticalStackPanel CreateItemWidget(string itemName, bool isFolder)
        {
#if DEBUG
            const string methodName = nameof(FileViewer) + "." + nameof(CreateItemWidget);
#endif
            ImageButton icon = new()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidBrush(Color.Black)
            };

            icon.TouchDown += (_, _) => _isRightClick = Mouse.GetState().RightButton == ButtonState.Pressed;

            if (isFolder)
            {
                icon.Click += (_, _) =>
                {
                    if (!_isRightClick)
                    {
                        CurrentDirectory = Path.Combine(CurrentDirectory, itemName);
                    }
                };
            }
            
            //context menu
            icon.Click += (_, _) =>
            {
                if (_isRightClick && Desktop.ContextMenu is null && itemName != "..")
                {
                    _selectedItemName = itemName;

                    Desktop.ShowContextMenu(isFolder ? _folderContextMenu : _fileContextMenu,
                        Desktop.TouchPosition ?? Point.Zero);
                }
            };

            string iconName =
                isFolder ? FolderIconName : IOHelper.ConvertExtensionToIconName(Path.GetExtension(itemName));
            if (_fileIcons.TryGetValue(iconName, out Texture2D? image))
            {
                icon.Image = new TextureRegion(image);
            }
            else
            {
                Debug.WriteLine($"{methodName}: cannot find icon of name {iconName}. Is Folder: {isFolder}");
            }

            return new VerticalStackPanel
            {
                Widgets =
                {
                    icon,
                    new TextBox
                    {
                        Text = itemName,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextVerticalAlignment = VerticalAlignment.Center,
                        Enabled = false,
                        DisabledTextColor = Color.White,
                        DisabledBackground = new SolidBrush(Color.Black),
                    },
                },
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private void BuildUI()
        {
            Widgets.Clear();

            Widget backFolderWidget = CreateItemWidget("..", true);
            backFolderWidget.GridColumn = 0;
            AddChild(backFolderWidget);

            int c = 1;
            int r = 0;
            
            foreach (string subDirectory in Directory.GetDirectories(CurrentDirectory))
            {
                Widget widget = CreateItemWidget(new DirectoryInfo(subDirectory).Name, true);
                widget.GridColumn = c++;
                widget.GridRow = r;

                if (c >= ItemsPerRow)
                {
                    c = 0;
                    r++;
                }
                
                AddChild(widget);
            }
            
            foreach (string filePath in Directory.GetFiles(CurrentDirectory))
            {
                Widget widget = CreateItemWidget(Path.GetFileName(filePath), false);
                widget.GridColumn = c++;
                widget.GridRow = r;

                if (c >= ItemsPerRow)
                {
                    c = 0;
                    r++;
                }
                
                AddChild(widget);
            }
        }

        private VerticalMenu CreateFileContextMenu()
        {
            VerticalMenu outMenu = new();
            
            MenuItem deleteItem = new() {Text = "Delete"};
            MenuItem renameItem = new() {Text = "Rename"};
            MenuItem editItem = new() {Text = "Edit"};
            MenuItem copyItem = new() {Text = "Copy into scene", Enabled = true};
            
            outMenu.Items.Add(deleteItem);
            outMenu.Items.Add(renameItem);
            outMenu.Items.Add(editItem);

            outMenu.Items.Add(copyItem);

            outMenu.VisibleChanged += (_, _) =>
            {
                copyItem.Enabled = IsSceneFile(_selectedItemName) && World is not null;
            };

            copyItem.Selected += (_, _) =>
            {
                CreateEntityNameEntryWindow(_selectedItemName).ShowModal(Desktop);
            };

            return outMenu;
        }

        private VerticalMenu CreateFolderContextMenu()
        {
            MenuItem deleteItem = new() {Text = "Delete"};
            MenuItem renameItem = new() {Text = "Rename"};

            return new VerticalMenu {Items = {deleteItem, renameItem}};
        }

        private Window CreateEntityNameEntryWindow(string entityName)
        {
            VerticalStackPanel stackPanel = new();
            Window outWindow = new() {Content = stackPanel};

            TextBox entryBox = new()
                {Text = Path.GetFileNameWithoutExtension(entityName), HorizontalAlignment = HorizontalAlignment.Center};
            TextButton okBox = new() {Text = "OK", HorizontalAlignment = HorizontalAlignment.Right};

            okBox.Click += (_, _) =>
            {
                outWindow.Close();

                if (World is null) return;

                string newName = entryBox.Text;
                string entityPath = Path.Combine(CurrentDirectory, entityName);
                string newEntityPath = Path.Combine(CurrentDirectory, $"{newName}.entity");

                string newContents = File.ReadAllText(entityPath);
                newContents = Regex.Replace(newContents, "(\"id\":\\s*\")(.+)(\")", $"$1{newName}$3");
                File.WriteAllText(newEntityPath, newContents);

                _globalCommands.AddCommandAndExecute(new AddEntityCommand(newEntityPath, newContents, World));
            };

            stackPanel.AddChild(new Label
                {Text = "Enter name of new entity:", HorizontalAlignment = HorizontalAlignment.Center});
            stackPanel.AddChild(entryBox);
            stackPanel.AddChild(okBox);

            return outWindow;
        }

        private static bool IsSceneFile(string fileName) => Path.GetExtension(fileName) is ".prefab" or ".entity";
    }
}