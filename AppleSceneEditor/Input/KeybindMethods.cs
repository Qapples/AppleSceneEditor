using System.Diagnostics;
using System.Linq;
using GrappleFightNET5.Scenes;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Input
{
    /// <summary>
    /// Methods that are activated from keybindings.
    /// </summary>
    public static class KeybindMethods
    {
        public static void Save(Widget root, Scene scene, object?[]? args)
        {
            if (args is null)
            {
                Debug.WriteLine($"Input.KeybindMethods.{nameof(Save)}: args parameter is null. Cannot save.");
                return;
            }

            ComponentPanelHandler? handler = null;
            foreach (var arg in args.Where(arg => arg is not null && arg.GetType() == typeof(ComponentPanelHandler)))
            {
                handler = arg as ComponentPanelHandler;
            }

            if (handler is null)
            {
                Debug.WriteLine($"Input.KeybindMethods.{nameof(Save)}: cannot find parameter of type " +
                                $"{typeof(ComponentPanelHandler)} in args! Cannot save.");
                return;
            }

            handler.SaveToScene(scene);
        }

        public static void New(Widget root, Scene scene, object?[]? args)
        {
        }

        public static void Open(Widget root, Scene scene, object?[]? args)
        {
        }
    }
}