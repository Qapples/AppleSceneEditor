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
        /// Indicates that the user wants to select an entity through the scene viewer and that a ray should be fired
        /// within the world
        /// </summary>
        FireEntitySelectionRay = 0b100,
        
        /// <summary>
        /// Indicates that a NEW entity has just been selected and things need to be updated.
        /// </summary>
        EntitySelected = 0b1000,
    }
}