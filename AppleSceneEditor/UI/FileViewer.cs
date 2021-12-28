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

        private VerticalStackPanel CreateFileItemWidget(string fileName)
        {
#if DEBUG
            const string methodName = nameof(FileViewer) + "." + nameof(CreateFileItemWidget);
#endif
            ImageButton icon = new()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidBrush(Color.Black)
            };
            
            if (_fileIcons.TryGetValue(IOHelper.ConvertExtensionToIconName(Path.GetExtension(fileName)),
                out IImage? iconImage))
            {
                icon.Image = iconImage;
            }
            else
            {
                Debug.WriteLine($"{methodName}: can't find icon for file viewer in {fileName}");
            }

            return new VerticalStackPanel
            {
                Widgets =
                {
                    icon,
                    new Label {Text = fileName, HorizontalAlignment = HorizontalAlignment.Center}
                },
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private const string FolderIconName = "folder_icon";
        
        private VerticalStackPanel CreateFolderItemWidget(string folderName)
        {
#if DEBUG
            const string methodName = nameof(FileViewer) + "." + nameof(CreateFolderItemWidget);
#endif
            ImageButton icon = new()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidBrush(Color.Black)
            };

            icon.Click += (_, _) =>
            {
                CurrentDirectory = Path.Combine(CurrentDirectory, folderName);
            };
            
            if (_fileIcons.TryGetValue(FolderIconName, out IImage? image))
            {
                icon.Image = image;
            }
            else
            {
                Debug.WriteLine($"{methodName}: cannot find folder icon whose name is supposed to be {FolderIconName}");
            }
            
            return new VerticalStackPanel
            {
                Widgets =
                {
                    icon,
                    new Label {Text = folderName, HorizontalAlignment = HorizontalAlignment.Center}
                },
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private void BuildUI()
        {
            InternalChild.Widgets.Clear();

            int i = 0;
            foreach (string subDirectory in Directory.GetDirectories(CurrentDirectory))
            {
                Widget widget = CreateFolderItemWidget(new DirectoryInfo(subDirectory).Name);
                widget.GridColumn = i++;
                
                InternalChild.AddChild(widget);
            }
            
            foreach (string filePath in Directory.GetFiles(CurrentDirectory))
            {
                Widget widget = CreateFileItemWidget(Path.GetFileName(filePath));
                widget.GridColumn = i++;
                
                InternalChild.AddChild(widget);
            }
        }
    }
}