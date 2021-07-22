using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AppleSceneEditor.Serialization.Converters
{
    /// <summary>
    /// Class used to convert string values found in Json files to Texture2D instances
    /// </summary>
    public class Texture2DJsonConverter : JsonConverter<Texture2D>
    {
        /// <summary>
        /// Given the name of a texture loaded through the content pipeline, or a Color via Color.{Color Name}
        /// returns a Texture2D object from a string value in Json files
        /// </summary>
        /// <param name="reader">JsonReader instance used to read data from the Json file</param>
        /// <param name="typeToConvert">The type that is being converted to (Texture2D)</param>
        /// <param name="options">The options of the JsonSerializer used</param>
        /// <returns>The Texture2D found by name, or a Color via Color.{Color Name}. If the string value from the Json
        /// object begins with "Color.", then a width * height box which is homogenous of the color specified is
        /// returned as a Texture. Otherwise, a texture via querying the ContentManager with the specified value is
        /// returned instead. If no Texture2D is found, then a black and pink checkerboard
        /// texture is used in place. If there is no size or scale property found within the Json object, a default
        /// size of 100x100 is used. (Bounds property is prioritized if both size and scale property exists)</returns>
        public override Texture2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            //First, try to parse a color value. If there is no color value, then get a Texture2D by name via the global
            //content manager

            Vector2? currentObjSize = GlobalVars.CurrentDeserializingObjectSize;
            var (width, height) = ((int) (currentObjSize?.X ?? 1), (int) (currentObjSize?.Y ?? 1));

            if (currentObjSize is null)
            {
                Debug.WriteLine($"{nameof(currentObjSize)} is null and the width and height of the texture will" +
                                $"have dimensions of 1 and 1");
            }
            
            string? value = reader.GetString();
            if (value is null)
            {
                Debug.WriteLine("Unable to get the string value when getting Texture2D. Using default texture");
                return TextureHelper.GenerateDefaultTexture(GlobalVars.GraphicsDevice, width, height);
            }

            Color? textureColor = TextureHelper.GetColorFromName(value.ToLower());

            if (textureColor is not null)
            {
                Color paramColor = textureColor.Value;

                //TextureFromColor is not case-sensitive, so we don't need to do anything extra to the string value
                return TextureHelper.CreateTextureFromColor(GlobalVars.GraphicsDevice, in paramColor, width, height);
            }

            //the string value is not a color, so intercept it as a name to a Texture2D existing within the content
            //manager
            Texture2D? outTexture = GlobalVars.GlobalContentManager.Load<Texture2D>(value);

            if (outTexture is not null)
            {
                return outTexture;
            }

            Debug.WriteLine($"Texture of name {value} was not found. Using default texture.");
            return TextureHelper.GenerateDefaultTexture(GlobalVars.GraphicsDevice, width, height);
            
        }

        /// <summary>
        /// Due to limitations and other roadblocks (such as the difficultly of expressing Texture data compactly in
        /// Json format) and the fact that this method will not be used often since most Json will be written by hand,
        /// this method is not implemented and will throw a NotImplementedException exception
        /// </summary>
        public override void Write(Utf8JsonWriter writer, Texture2D value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}