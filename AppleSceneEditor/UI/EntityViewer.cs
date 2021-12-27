using System.Collections.Generic;
using AppleSceneEditor.Commands;
using AppleSceneEditor.ComponentFlags;
using AppleSerialization.Json;
using DefaultEcs;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.UI
{
    public class EntityViewer
    {
        public const string EntityButtonIdPrefix = "EntityButton_";
        
        public StackPanel EntityButtonStackPanel { get; set; }
        
        public World World { get; set; }
        
        public List<JsonObject> EntityJsonObjects { get; set; }
        
        public string EntitiesDirectory { get; set; }

        private CommandStream _commands;

        public EntityViewer()
        {
        }

        public void PopulatePanel(string entitiesDirectory)
        {
        }

        private Grid CreateEntityButtonGrid(string id, Entity entity)
        {
            Grid outGrid = new()
            {
                ColumnSpacing = 4,
                RowSpacing = 4,
                Id = $"EntityGrid_{id}"
            };

            outGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            outGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            outGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            outGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            
            TextButton entityButton = new() {Text = id, Id = $"{EntityButtonIdPrefix}_{id}"};
            entityButton.TouchDown += (_, _) =>
            {
                GlobalFlag.SetFlag(GlobalFlags.EntitySelected, true);
                World.Set(new SelectedEntityFlag(entity));
            };

            return outGrid;
        }
    }
}