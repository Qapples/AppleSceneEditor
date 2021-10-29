using System;
using System.Collections.Generic;
using AppleSerialization.Json;

namespace AppleSceneEditor
{
    public partial class MainGame
    {
        private static readonly Dictionary<Type, JsonObject> Prototypes = new();

        /// <summary>
        /// Static method that setups Prototypes.
        /// </summary>
        static MainGame()
        {
            // 
        }
    }
}