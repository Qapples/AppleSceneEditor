using AppleSerialization.Json;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace AppleSceneEditor.Extensions
{
    public class MyraExtensions
    {
        private const string ComponentGridId = "ComponentGrid";
        
        public static Grid CreateComponentGrid(JsonObject obj, Panel widgetsPanel, string header)
        {
            widgetsPanel.GridRow = 1;
            widgetsPanel.GridColumn = 1;

            Grid outGrid = new()
            {
                ColumnSpacing = 4,
                RowSpacing = 4,
                Id = ComponentGridId
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
                    outGrid.AddChild(widgetsPanel);
                }
                else
                {
                    outGrid.RemoveChild(widgetsPanel);
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

            TextButton removeButton = new()
            {
                Text = "-",
                HorizontalAlignment = HorizontalAlignment.Right,
                GridColumn = 1
            };

            removeButton.Click += (_, _) =>
            {
                JsonArray? componentArray = obj.Parent?.FindArray("components");
                componentArray?.Remove(obj);
                
                outGrid.RemoveFromParent();
                outGrid = null;
            };

            outGrid.AddChild(removeButton);

            return outGrid;
        }
    }
}