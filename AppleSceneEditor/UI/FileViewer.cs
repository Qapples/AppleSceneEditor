using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AppleSceneEditor.Extensions;
using FastDeepCloner;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace AppleSceneEditor.UI
{
    public sealed class FileViewer : SingleItemContainer<Grid>
    {
        public string CurrentDirectory { get; set; }
        
        public int ItemsPerRow { get; set; }

        private readonly Dictionary<string, Image> _fileIcons;

        public FileViewer(TreeStyle? style, string directory, int itemsPerRow, Dictionary<string, Image> fileIcons)
        {
            (CurrentDirectory, ItemsPerRow, _fileIcons) = (directory, itemsPerRow, fileIcons);

            InternalChild = new Grid
            {
                ColumnSpacing = 4,
                RowSpacing = 4,
            };

            int i;
            for (i = 0; i < itemsPerRow; i++)
            {
                //myra is wack. Proportions are over 3 instead of over 1.
                InternalChild.ColumnsProportions.Add(new Proportion(ProportionType.Part, 3f / itemsPerRow));
            }

            i = 0;
            foreach (string subDirectory in Directory.GetDirectories(directory))
            {
                Widget widget = CreateFolderItemWidget(new DirectoryInfo(subDirectory).Name);
                widget.GridColumn = i++;
                
                InternalChild.AddChild(widget);
            }
            
            foreach (string filePath in Directory.GetFiles(directory))
            {
                Widget widget = CreateFileItemWidget(Path.GetFileName(filePath));
                widget.GridColumn = i++;
                
                InternalChild.AddChild(widget);
            }

            if (style is not null)
            {
                ApplyWidgetStyle(style);
            }

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }

        public FileViewer(string directory, int itemsPerRow, Dictionary<string, Image> fileIcons) : this(
            Stylesheet.Current.TreeStyle, directory, itemsPerRow, fileIcons)
        {
        }

        private VerticalStackPanel CreateFileItemWidget(string fileName)
        {
#if DEBUG
            const string methodName = nameof(FileViewer) + "." + nameof(CreateFileItemWidget);
#endif
            if (!_fileIcons.TryGetValue(IOHelper.ConvertExtensionToIconName(Path.GetExtension(fileName)),
                out Image? icon))
            {
                icon = new Image();
                Debug.WriteLine($"{methodName}: can't find icon for file viewer in {fileName}");
            }

            //do a shallow copy of the icon.
            icon = new Image {Renderable = icon.Renderable, HorizontalAlignment = HorizontalAlignment.Center};

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
            if (!_fileIcons.TryGetValue(FolderIconName, out Image? icon))
            {
                icon = new Image();
                Debug.WriteLine($"{methodName}: cannot find folder icon whose name is supposed to be {FolderIconName}");
            }
            
            //do a shallow copy of the icon.
            icon = new Image {Renderable = icon.Renderable, HorizontalAlignment = HorizontalAlignment.Center};

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
    }
}