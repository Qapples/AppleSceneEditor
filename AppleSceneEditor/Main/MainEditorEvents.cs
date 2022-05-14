using System;
using System.Diagnostics;
using System.Linq;
using AppleSceneEditor.Commands;
using Myra.Graphics2D.UI.File;

namespace AppleSceneEditor
{
    public partial class MainGame
    {
        //-----------------
        // EVENT METHODS
        //-----------------

        private FileDialog _openFileDialog;
        private FileDialog _newFileDialog;

        public void MenuFileOpen(object? sender, EventArgs? eventArgs)
        {
            GlobalFlag.SetFlag(GlobalFlags.UserControllingSceneViewer, false);
            _openFileDialog.ShowModal(_desktop);
        }

        public void MenuFileNew(object? sender, EventArgs? eventArgs)
        {
            GlobalFlag.SetFlag(GlobalFlags.UserControllingSceneViewer, false);
            _newFileDialog.ShowModal(_desktop);
        }

        public void SettingsMenuOpen(object? sender, EventArgs? eventArgs)
        {
            GlobalFlag.SetFlag(GlobalFlags.UserControllingSceneViewer, false);
            _settingsWindow.ShowModal(_desktop);
        }

        public void HitboxEditorOpen(object? sender, EventArgs? eventArgs)
        {
            GlobalFlag.SetFlag(GlobalFlags.UserControllingSceneViewer, false);
            _hitboxEditorWindow.Show(_desktop);
        }

        private void AddComponentButtonClick(object? sender, EventArgs? eventArgs)
        {
            if (_currentJsonObject is null) return;

            _addComponentWindow.ShowModal(_desktop);
        }

        private void FinishButtonClick(string typeName)
        {
            const string methodName = nameof(MainGame) + "." + nameof(FinishButtonClick) + " (EditorEvents)";

            _addComponentWindow.Close();

            if (_currentJsonObject is null || _mainPanelHandler is null) return;

            if (!_prototypes.TryGetValue(typeName, out var prototype))
            {
                Debug.WriteLine($"{methodName}: cannot find component prototype of name {typeName}! Cannot create" +
                                "new component");
                return;
            }

            if (_mainPanelHandler.Components.Any(e => e.FindProperty("$type")?.Value as string == typeName))
            {
                Debug.WriteLine($"{methodName}: component of type {typeName} already exists in the currently " +
                                $"selected entity!");

                _alreadyExistsWindow.ShowModal(_desktop);

                return;
            }
            
            _commands.AddCommandAndExecute(new AddComponentCommand(prototype, _mainPanelHandler));
        }
    }
}