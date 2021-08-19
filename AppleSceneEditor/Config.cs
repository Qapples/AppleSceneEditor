using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GrappleFightNET5.Scenes;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor
{
    /// <summary>
    /// Contains data that can be changed externally through files.
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// Contains all the possible function names that can be associated with keybindings and be .
        /// </summary>
        public static readonly string[] ValidFunctionNames = {"save", "new", "open"};

        /// <summary>
        /// A dictionary containing user changeable keybindings for specific functions like saving, opening a new file,
        /// etc. The key in this context is the name of the function defined in a config file.
        /// </summary>
        public static Dictionary<string, List<List<Keys>>> Keybinds { get; set; } = new();

        /// <summary>
        /// Parses a file and adjusts the data stored in the static <see cref="Config"/> type accordingly.
        /// </summary>
        /// <param name="configFileContents">The CONTENTS of the config file.</param>
        public static void ParseConfigFile(string configFileContents)
        {
            int lineNum = 0;
            foreach (string line in configFileContents.Split('\n'))
            {
                string[] splitColon = line.Split(':');

                if (splitColon.Length > 1)
                {
                    string functionName = splitColon[0];
                    IEnumerable<string> keySplit = splitColon[1].Split(' ').Skip(1);

                    List<Keys> keys = new();
                    foreach (string keyStr in keySplit)
                    {
                        if (Enum.TryParse(keyStr, out Keys key))
                        {
                            keys.Add(key);
                        }
                        else
                        {
                            Debug.WriteLine($"{nameof(ParseConfigFile)}: Cannot parse key ({keyStr}) on line#{lineNum}." +
                                            $"\nLine contents:{line}");
                        }
                    }

                    if (Keybinds.TryGetValue(functionName, out var keysList))
                    {
                        keysList.Add(keys);
                    }
                    else
                    {
                        Keybinds.Add(functionName, new List<List<Keys>> {keys});
                    }
                }
                else
                {
                    Debug.WriteLine($"{nameof(ParseConfigFile)}: Line #{lineNum} in config file cannot be parsed. Line " +
                                    $"contents: {line}");
                }

                lineNum++;
            }
        }
    }
}