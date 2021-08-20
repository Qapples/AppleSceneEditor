using System;
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

            ComponentPanelHandler? handler = FindArg<ComponentPanelHandler>(args);
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
            if (args is null)
            {
                Debug.WriteLine($"Input.KeybindMethods.{nameof(New)}: args parameter is null. Cannot init new.");
                return;
            }

            MainGame? game = FindArg<MainGame>(args);

            if (game is null)
            {
                Debug.WriteLine($"Input.KeybindMethods.{nameof(New)}: cannot find parameter of type " +
                                $"{typeof(MainGame)} in args! Cannot save.");
                return;
            }
            
            game.MenuFileNew(null, null);
        }

        public static void Open(Widget root, Scene scene, object?[]? args)
        {
            if (args is null)
            {
                Debug.WriteLine($"Input.KeybindMethods.{nameof(Open)}: args parameter is null. Cannot open.");
                return;
            }

            MainGame? game = FindArg<MainGame>(args);

            if (game is null)
            {
                Debug.WriteLine($"Input.KeybindMethods.{nameof(Open)}: cannot find parameter of type " +
                                $"{typeof(MainGame)} in args! Cannot open.");
                return;
            }

            game.MenuFileOpen(null, null);
        }

        private static T? FindArg<T>(object?[]? args) =>
            (T?) args?.FirstOrDefault(arg => arg is not null && arg.GetType() == typeof(T));
    }
}