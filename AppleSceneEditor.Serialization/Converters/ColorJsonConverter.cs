using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace AppleSceneEditor.Serialization.Converters
{
    /// <summary>
    /// Class used to convert string values found in Json files to Color instances
    /// </summary>
    public class ColorJsonConverter : JsonConverter<Color>
    {
        /// <summary>
        /// From the name of a color ("green", "red", etc.) in a Json context, return the appropriate Color struct value
        /// If the value provided by the JsonReader is null, then the default value returned will be Color.Transparent
        /// If the name is invalid, the default value will also be Color.Transparent.
        /// If you do not want to use the static const values provided by the Color class, then explicitly set the
        /// R, G, B, A values.
        /// </summary>
        /// <param name="reader">Reader struct provided to read the value</param>
        /// <param name="typeToConvert">The type value that is being converted (should be Color)</param>
        /// <param name="options">The options of the JsonSerializer used</param>
        /// <returns>The appropriate Color value based on the name given</returns>
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            if (value is null)
            {
                Debug.WriteLine(
                    "Unable to obtain string value in reading Color. Using default Color.Transparent value");
                return Color.Transparent;
            }

            Color? color = TextureHelper.GetColorFromName(value);
            if (color is null)
            {
                Debug.WriteLine($"Color of name {value} is invalid. Using default Color.Transparent value");
                return Color.Transparent;
            }

            return color.Value;
        }

        /// <summary>
        /// Given a color value, write the appropriate Json string using {"R":{R}, "G":{G}, "B":{B}, "A":{A}}
        /// </summary>
        /// <param name="writer">Object used to write the Json </param>
        /// <param name="value">The color value to write </param>
        /// <param name="options">The options of the JsonSerializer used</param>
        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) =>
            writer.WriteStringValue($"\"R\":{value.R} \"G\":{value.G} \"B\":{value.B} \"A\":{value.A}");
    }
}