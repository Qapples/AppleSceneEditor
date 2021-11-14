using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Extensions
{
    //TODO: Add docs
    public static class KeyboardExtensions
    {
        public static Keys[] ParseKeyboardState(string str)
        {
#if DEBUG
            const string methodName = nameof(KeyboardExtensions) + "." + nameof(ParseKeyboardState);
#endif
            
            string[] splitStr = str.Split(' ');
            Keys[] keys = new Keys[splitStr.Length];

            for (int i = 0; i < splitStr.Length; i++)
            {
                if (TryParseKey(splitStr[i], out var key))
                {
                    keys[i] = key;
                }
                else
                {
                    Debug.WriteLine($"{methodName}: unable to parse key from string {str} at index {i}. " +
                                    $"Returning empty array.");
                    return Array.Empty<Keys>();
                }
            }

            return keys;
        }

        public static bool TryParseKey(string str, out Keys key) => Enum.TryParse<Keys>(str, out key);
    }
}