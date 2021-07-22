using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AppleSceneEditor.Serialization
{
    public static class GlobalVars
    {
        //Neither of these variables are going to be null after initialization in the game method
#nullable disable

        /// <summary>
        /// Content path were all assets are stored. Can be changed though program parameters.
        /// </summary>
        public static string ContentPath { get; set; }

        /// <summary>
        /// When an invalid SpriteFont is invalid, this sprite font will be used instead
        /// </summary>
        public static FontSystem DefaultFontSystem { get; set; }

        /// <summary>
        /// Main graphics device used for rendering
        /// </summary>
        public static GraphicsDevice GraphicsDevice { get; set; }

        /// <summary>
        /// ContentManager that can be used globally so that content can be loaded regardless of location
        /// </summary>
        public static RawContentManager GlobalContentManager { get; set; }

        /// <summary>
        /// Represents the size of the object currently being deserialized. Used for generating Texture2D when a size
        /// is needed. If null, then no object has been deserialized. Only intended to be used in the Serializer class
        /// and not intended to be manipulated by users or other parts of the program
        /// </summary>
        // This is a very hacky and stupid solution and there may or may not be a better solution, but it works.
        public static Vector2? CurrentDeserializingObjectSize { get; set; } 
#nullable enable
    }
}