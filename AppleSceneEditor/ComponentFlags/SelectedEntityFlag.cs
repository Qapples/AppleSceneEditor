using DefaultEcs;

namespace AppleSceneEditor.ComponentFlags
{
    public readonly struct SelectedEntityFlag
    {
        public readonly Entity SelectedEntity;
        
        public SelectedEntityFlag(Entity selectedEntity) => (SelectedEntity) = (selectedEntity);
    }
}