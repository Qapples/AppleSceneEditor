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
        UserControllingSceneViewer = 0b10
    }
}