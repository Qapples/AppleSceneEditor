using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace AppleSceneEditor.UI.HitboxEditor
{
    public class HitboxEditor : SingleItemContainer<Grid>
    {
        public string _hitboxFilePath;

        public string HitboxFilePath
        {
            get => _hitboxFilePath;
            set
            {
                _hitboxFilePath = value;
                LoadHitboxFile(value);
            }
        }

        private VerticalStackPanel _opcodesStackPanel;
        private VerticalStackPanel _hullsStackPanel;
        
        private ob[] _opcode
        
        public HitboxEditor(TreeStyle? style, string hitboxFilePath)
        {
            if (style is not null)
            {
                ApplyWidgetStyle(style);
            }

            InternalChild = new Grid();

            InternalChild.ColumnsProportions.Add(new Proportion(ProportionType.Part, 2.5f));
            InternalChild.RowsProportions.Add(new Proportion(ProportionType.Part, 0.5f));

            _opcodesStackPanel = new VerticalStackPanel {GridColumn = 1, GridRow = 1};
            _hullsStackPanel = new VerticalStackPanel {GridColumn = 1, GridRow = 0};
            
            InternalChild.AddChild(_opcodesStackPanel);
            InternalChild.AddChild(_hullsStackPanel);

            HitboxFilePath = hitboxFilePath;
        }

        public HitboxEditor(string hitboxFilePath) : this(Stylesheet.Current.TreeStyle, hitboxFilePath)
        {
        }

        public void LoadHitboxFile(string hitboxFilePath)
        {
            
        }
    }
}