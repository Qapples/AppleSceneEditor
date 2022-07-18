using System;
using System.Diagnostics;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using Myra.MML;

namespace AppleSceneEditor.Extensions
{
    public static class MyraExtensions
    {
        public static T? TryFindWidgetById<T>(this Container container, string id) where T : Widget
        {
#if DEBUG
            const string methodName = nameof(MyraExtensions) + "." + nameof(TryFindWidgetById);
#endif
            T? output;
            
            try
            {
                output = container.FindWidgetById(id) as T;

                if (output is null)
                {
                    Debug.WriteLine($"{methodName}: {id} cannot be casted into an instance of {typeof(T)}");
                    return null;
                }
            }
            catch
            {
                Debug.WriteLine($"{methodName}: {typeof(T)} of ID {id} cannot be found.");
                return null;
            }

            return output;
        }

        public static Grid CreateDropDown<T>(T widgetsContainer, string header, string gridId,
            EventHandler? onRemoveClick = null) where T : Widget, IMultipleItemsContainer
        {
            widgetsContainer.GridRow = 1;
            widgetsContainer.GridColumn = 1;

            Grid outGrid = new()
            {
                ColumnSpacing = 4,
                RowSpacing = 4,
                Id = gridId
            };
            
            outGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            outGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            outGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            outGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            
            ImageButton mark = new(null)
            {
                Toggleable = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            mark.PressedChanged += (_, _) =>
            {
                if (mark.IsPressed)
                {
                    outGrid.AddChild(widgetsContainer);
                }
                else
                {
                    outGrid.RemoveChild(widgetsContainer);
                }
            };
            
            mark.ApplyImageButtonStyle(Stylesheet.Current.TreeStyle.MarkStyle);
            outGrid.AddChild(mark);

            Label label = new(null)
            {
                Text = header,
                GridColumn = 1
            };
            
            label.ApplyLabelStyle(Stylesheet.Current.TreeStyle.LabelStyle);
            outGrid.AddChild(label);

            if (onRemoveClick is null)
            {
                return outGrid;
            }

            //only add the remove button if a onRemoveClick is specified
            TextButton removeButton = new()
            {
                Text = "-",
                HorizontalAlignment = HorizontalAlignment.Right,
                GridColumn = 1
            };

            removeButton.Click += onRemoveClick;
            outGrid.AddChild(removeButton);

            return outGrid;
        }
    }
}