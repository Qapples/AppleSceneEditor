using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GrappleFightNET5.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor
{
    /// <summary>
    /// Provides assistance in working with keybindings and keyboard input.
    /// </summary>
    public static class Input
    {
        /// <summary>
        /// The <see cref="KeyboardState"/> from the previous update. Ensure that this value is set to at the end of
        /// the update method in the main game logic.
        /// </summary>
        public static KeyboardState PreviousKeyboardState { get; set; }

        /// <summary>
        /// Delegate that is used in <see cref="Input.KeyFunctions"/>
        /// </summary>
        public delegate void KeybindDelegate(Widget root, Scene scene);

        /// <summary>
        /// Defines the behavior of keybindings. The string key represents the name of the function and a
        /// <see cref="KeybindDelegate"/> is returned which describes what happens when that keybind is active. 
        /// </summary>
        public static readonly Dictionary<string, KeybindDelegate> KeyFunctions = new()
        {
            {"save", (root, scene) => { Debug.WriteLine("Save!"); }},
            {"new", (root, scene) => { Debug.WriteLine("New!"); }},
            {"open", (root, scene) => { Debug.WriteLine("Open!"); }},
        };

        /// <summary>
        /// Updates input logic. Checks for if any keybindings are to be activated.
        /// </summary>
        /// <param name="currentState">The current state provided by <see cref="Keyboard"/> in the main game loop.
        /// </param>
        /// <param name="rootWidget">The root widget of the current <see cref="Desktop"/> that is active in the main
        /// game. This is provided so that the functions in <see cref="KeyFunctions"/> can manipulate it for purposes
        /// like saving, opening a new file, etc.</param>
        /// <param name="currentScene">The current <see cref="Scene"/> that is active in the main game.</param>
        /// <param name="gameTime">The current <see cref="GameTime"/> provided in the
        /// <see cref="Game.Update"/> or <see cref="Game.Draw"/> methods.</param>
        public static void Update(in KeyboardState currentState, Widget rootWidget, Scene currentScene,
            GameTime gameTime)
        {
            foreach (string functionName in Config.ValidFunctionNames)
            {
                if (!Config.Keybinds.TryGetValue(functionName, out var keyLists))
                {
                    Debug.WriteLine($"Input.{nameof(Update)}: cannot find keybind of function name {functionName}");
                    continue;
                }
                
                //it takes a while for a keystroke combination to be registered and acknowledged. unsure on why this is
                //the case as actual performance does not take any noticeable hit. not a very big deal but maybe this
                //is something to fix later on.
                foreach (List<Keys> keys in keyLists)
                {
                    if (keys.All(currentState.IsKeyDown) && !keys.All(PreviousKeyboardState.IsKeyDown))
                    {
                        if (!KeyFunctions.TryGetValue(functionName, out var keybindDelegate))
                        {
                            Debug.WriteLine($"Input.{nameof(Update)}: cannot find KeyFunction of name {functionName}");
                            continue;
                        }

                        keybindDelegate(rootWidget, currentScene);
                    }
                }
            }
        }
    }
}