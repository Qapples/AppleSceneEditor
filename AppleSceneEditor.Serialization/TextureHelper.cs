using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace AppleSceneEditor.Serialization
{
    public static class TextureHelper
    {
        /// <summary>
        /// Creates a Texture2D object that is a width x height rectangle of a specified homogenous color 
        /// </summary>
        /// <param name="graphicsDevice">GraphicsDevice object used to create the Texture2D instance</param>
        /// <param name="color">Color of the texture</param>
        /// <param name="width">Width of the texture</param>
        /// <param name="height">Height of the texture</param>
        /// <returns>A Texture2D object of width x height dimensions that is a rectangle of a homogenous color</returns>
        public static Texture2D CreateTextureFromColor(GraphicsDevice graphicsDevice, in Color color, int width, int height)
        {
            Texture2D outTexture = new(graphicsDevice, width, height);
             
            Color[] colorArr = new Color[width * height];
            for (int i = 0; i < width * height; i++) colorArr[i] = color;
            
            outTexture.SetData(colorArr);

            return outTexture;
        }

        /// <summary>
        /// Given the name of a Color, generate a texture of specified width and height that is a box that is homogenous
        /// of the color specified
        /// </summary>
        /// <param name="graphicsDevice">GraphicsDevice object used to create the Texture2D instance</param>
        /// <param name="nameOfColor">Name of the Color. All possible Colors can be found in the Color struct.
        /// if the color cannot be found, then by default it will be transparent</param>
        /// <param name="width">Width of the texture</param>
        /// <param name="height">Height of the texture</param>
        /// <returns>A Texture2D object of width x height dimensions that is a rectangle of a homogenous color. If the
        /// color is not found, a transparent texture is returned</returns>
        public static Texture2D CreateTextureFromNameOfColor(GraphicsDevice graphicsDevice, string nameOfColor,
            int width, int height)
        {
            Color? color = GetColorFromName(nameOfColor);
            if (color is null)
            {
                Debug.WriteLine($"Color of name {nameOfColor} cannot be found in the Color struct. By default, " +
                                $" the color of the texture will be entirely transparent");
                return CreateTextureFromColor(graphicsDevice, Color.Transparent, width, height);
            }

            return CreateTextureFromColor(graphicsDevice, color.Value, width, height);
        }

        /// <summary>
        /// Returns a Texture2D instance from the name of a texture via a ContentManager or the name of a color
        /// </summary>
        /// <param name="graphicsDevice">GraphicsDevice instance used to create a texture if the name parameter
        /// is a color</param>
        /// <param name="contentManager">ContentManager instance used to load texture if the name parameter represents
        /// the name of a texture</param>
        /// <param name="name">Name of the texture the load or the color to create a texture out of</param>
        /// <param name="width">Width of the texture</param>
        /// <param name="height">Height of the texture</param>
        /// <returns>A texture that is loaded from the name. If it is the name of the color, a width x height (both are
        /// 1 unless specified otherwise) homogenous texture of that color is returned</returns>
        public static Texture2D GetTextureFromNameOrColor(GraphicsDevice graphicsDevice, ContentManager contentManager,
            string name, int width = 1, int height = 1)
        {
            try
            {
                return contentManager.Load<Texture2D>(name);
            }
            catch
            {
                Color? textureColor = GetColorFromName(name);

                if (textureColor is not null)
                {
                    return CreateTextureFromNameOfColor(graphicsDevice, name, width, height);
                }
                
                Debug.WriteLine($"Texture of name or color {name} cannot be found. Returning default texture");

                return GenerateDefaultTexture(graphicsDevice, width, height);
            }
        }

        /// <summary>
        /// Given a string, find the corresponding static Color property with the same name as the given string
        /// </summary>
        /// <param name="colorName">Name of the color to get</param>
        /// <returns>If the property exists, the Color with the same name as colorName is returned. If no such Color
        /// exists, then null is returned</returns>
        public static Color? GetColorFromName(string colorName)
        {
            //ensure case-insensitivity
            colorName = colorName.ToLower();

            int colorStrIndex = colorName.IndexOf("color.");
            if (colorStrIndex > -1) colorName = colorName[(colorStrIndex + 6)..];
            
            colorName = char.ToUpper(colorName[0]) + colorName[1..];

            //use reflection to access the const colorNames in the Color class. If the name is invalid, then return
            //Color.Transparent
            var property = typeof(Color).GetProperty(colorName, BindingFlags.Static | BindingFlags.Public)
                ?.GetValue(null);

            return (Color?) property;
        }

        /// <summary>
        /// Generates a pink and black colored texture that is displayed for when a texture is not found
        /// </summary>
        /// <param name="graphicsDevice">GraphicsDevice object used to create the texture</param>
        /// <param name="width">The width of the texture</param>
        /// <param name="height">The height of the texture</param>
        /// <returns>A Texture2D object that represents a black and pink texture of specified width and height
        /// </returns>
        public static Texture2D GenerateDefaultTexture(GraphicsDevice graphicsDevice, int width, int height)
        {
            Texture2D outTexture = new(graphicsDevice, width, height);
            Color[] textureColor = new Color[width * height];

            var (midWidth, midHeight) = (width / 2, height / 2);

            //the texture will appear like the one in the Source Engine. It will be separated into four quadrants, with
            //top left and bottom right being pink, and the top right and bottom left being right
            for (int i = 0; i < textureColor.Length; i++)
            {
                var (r, c) = (textureColor.Length / width, textureColor.Length % height);

                textureColor[i] = (r < midHeight && c < midWidth) || (r > midHeight && c > midWidth)
                    ? Color.Pink
                    : Color.Black;
            }

            outTexture.SetData(textureColor);

            return outTexture;
        }
    }
}