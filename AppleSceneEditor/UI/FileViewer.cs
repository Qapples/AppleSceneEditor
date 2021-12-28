using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AppleSceneEditor.Extensions;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace AppleSceneEditor.UI
{
    public sealed class FileViewer : SingleItemContainer<Grid>
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

        private readonly Dictionary<string, IImage> _fileIcons;

        public FileViewer(TreeStyle? style, string directory, int itemsPerRow, Dictionary<string, IImage> fileIcons)
        {
            (_currentDirectory, ItemsPerRow, _fileIcons) = (directory, itemsPerRow, fileIcons);

            InternalChild = new Grid
            {
                ColumnSpacing = 4,
                RowSpacing = 4,
            };

            if (style is not null)
            {
                ApplyWidgetStyle(style);
            }

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            
            for (int i = 0; i < ItemsPerRow; i++)
            {
                //myra is wack. Proportions are over 3 instead of over 1.
                InternalChild.ColumnsProportions.Add(new Proportion(ProportionType.Part, 3f / ItemsPerRow));
            }
            
            BuildUI();
        }

        public FileViewer(string directory, int itemsPerRow, Dictionary<string, IImage> fileIcons) : this(
            Stylesheet.Current.TreeStyle, directory, itemsPerRow, fileIcons)
        {
        }

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

            if (isFolder)
            {
                icon.Click += (_, _) => { CurrentDirectory = Path.Combine(CurrentDirectory, itemName); };
            }

            string iconName =
                isFolder ? FolderIconName : IOHelper.ConvertExtensionToIconName(Path.GetExtension(itemName));
            if (_fileIcons.TryGetValue(iconName, out IImage? image))
            {
                icon.Image = image;
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
            InternalChild.Widgets.Clear();

            Widget backFolderWidget = CreateItemWidget("..", true);
            backFolderWidget.GridColumn = 0;
            InternalChild.AddChild(backFolderWidget);

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
                
                InternalChild.AddChild(widget);
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
                
                InternalChild.AddChild(widget);
            }
        }
    }
}