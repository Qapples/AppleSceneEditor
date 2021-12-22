namespace AppleSceneEditor
{
    public enum GlobalFlags
    {
        /// <summary>
        /// Indicates that the keybindings have been updated
        /// </summary>
        KeybindUpdated = 0b1,

        /// <summary>
        /// Indicates that the user is controlling the camera in the scene viewer
        /// </summary>
        UserControllingSceneViewer = 0b10,
        
        /// <summary>
        /// Indicates that the user wants to fire a ray within the scene editor to either select an entity or to
        /// manipulate the transformation of an entity via the transformation axis.
        /// </summary>
        FireSceneEditorRay = 0b100,
        
        /// <summary>
        /// Indicates that a NEW entity has just been selected and things need to be updated.
        /// </summary>
        EntitySelected = 0b1000,
    }
}