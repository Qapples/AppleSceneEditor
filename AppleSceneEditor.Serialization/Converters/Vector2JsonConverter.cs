using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace AppleSceneEditor.Serialization.Converters
{
    /// <summary>
    /// Class used to convert string values to Vector2 values from Json objects
    /// </summary>
    public class Vector2JsonConverter : JsonConverter<Vector2>
    {
        /// <summary>
        /// Given a string value from a Json value in the format of "x y" where parentheses or commas
        /// can be included and spaces unnecessary) return an instance of Vector2 that is representative of the
        /// string value. If the value is unable to be parsed, then this will return a value of 0 0
        /// </summary>
        /// <param name="reader">Utf8JsonReader instance used to read data from a Json file</param>
        /// <param name="typeToConvert">Type to convert to (Vector2)</param>
        /// <param name="options">The options of the JsonSerializer used</param>
        /// <returns>A Vector2 instance that is representative of the string value given. Returns 0 0 if unsuccessful to
        /// parse</returns>
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? readerStr = reader.GetString();
            if (readerStr is null)
            {
                Debug.WriteLine("Unable to READ Vector2 value. Using default value of (0, 0)");
                return new Vector2(0, 0);
            }

            //ignore spaces and parentheses by only taking the actual values into consideration and ignoring unneeded
            //values
            if (!GetNumFromStrVector(readerStr, 0, out float x, out int i))
            {
                Debug.WriteLine($"Unable to parse X value of {readerStr}. Returning default value of (0, 0)");
                return new Vector2(0, 0);
            }

            if (!GetNumFromStrVector(readerStr, i, out float y, out _))
            {
                Debug.WriteLine($"Unable to parse Y value of {readerStr}. Returning default value of (0, 0)");
                return new Vector2(0, 0);
            }

            return new Vector2(x, y);
        }

        /// <summary>
        /// Writes a Vector2 value to a Json object in the format of "X Y"
        /// </summary>
        /// <param name="writer">Utf8JsonWriter instance used to write to a Json object</param>
        /// <param name="value">Value to write</param>
        /// <param name="options">The options of the JsonSerializer used</param>
        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options) =>
            writer.WriteStringValue($"{value.X} {value.Y}");

        private bool IsNumChar(char c) => c == '.' || char.IsDigit(c);

        private bool GetNumFromStrVector(string str, int startIndex, out float output, out int endIndex)
        {
            //ignore spaces and parentheses by only taking the actual values into consideration and ignoring unneeded
            //values
            
            int i = startIndex;
            string strToParse = "";

            bool addToStr = false;
            for (; i < str.Length; i++)
            {
                char c = str[i];

                if (IsNumChar(c))
                {
                    addToStr = true;
                    strToParse += c;
                }
                else if (addToStr)
                {
                    break;
                }
            }

            endIndex = i;
            return float.TryParse(strToParse, out output);
        }
    }
}