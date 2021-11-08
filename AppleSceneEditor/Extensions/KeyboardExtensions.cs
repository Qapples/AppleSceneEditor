using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Extensions
{
    //TODO: Add docs
    public static class KeyboardExtensions
    {
        public static KeyboardState ParseString(string str)
        {
#if DEBUG
            const string methodName = nameof(KeyboardExtensions) + "." + nameof(ParseString);
#endif
            
            string[] splitStr = str.Split(' ');
            Keys[] keys = new Keys[splitStr.Length];

            for (int i = 0; i < splitStr.Length; i++)
            {
                if (!TryParseKey(splitStr[i], out keys[i]))
                {
                    keys[i] = Keys.None;

                    Debug.WriteLine($"{methodName}: unable to parse key from string {str} at index {i}. Default " +
                                    $"to Keys.None");
                }
            }

            return new KeyboardState(keys);
        }

        public static bool TryParseKey(string str, out Keys key) => Enum.TryParse<Keys>(str, out key);
    }
}