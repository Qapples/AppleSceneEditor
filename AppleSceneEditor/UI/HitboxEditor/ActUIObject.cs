using System;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace AppleSceneEditor.UI.HitboxEditor
{
    public class ActUIObject : SingleItemContainer<Grid>
    {
        public float Time { get; set; }
        
        public ActUIObject(TreeStyle? style)
        {
            
        }

        public ActUIObject() : this(Stylesheet.Current.TreeStyle)
        {
            
        }

        public void ToBytes(in Span<byte> bytesDestination)
        {
            
        }
    }
}   