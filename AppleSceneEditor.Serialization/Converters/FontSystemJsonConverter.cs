using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FontStashSharp;

namespace AppleSceneEditor.Serialization.Converters
{
    /// <summary>
    /// Class used to convert string values found in Json files to FontSystems 
    /// </summary>
    public class FontSystemJsonConverter : JsonConverter<FontSystem>
    {
        /// <summary>
        /// Given an array of directories provided by a Utf8JsonReader: loads multiple fonts and creates a FontSystem
        /// with them. Note that the array of directories MUST have the first element be a string representing the
        /// name of the FontSystem to load the fonts under.
        /// </summary>
        /// <param name="reader">JsonReader instance used to read data from the Json file. The data should be in the
        /// from of an array, and the first element represents the name to load the fonts under.</param>
        /// <param name="typeToConvert">The type that is being converted to (FontSystem).</param>
        /// <param name="options">The options of the JsonSerializer used.</param>
        /// <returns>If successful, a FontSystem that contains the loaded fonts from the specified directories is
        /// returned. If unsuccessful, GlobalVars.DefaultFontSystem is returned and a message to the debug console
        /// is written to.</returns>
        public override FontSystem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                object?[]? objPaths = ConverterHelper.GetArrayFromReader(ref reader, options);

                if (objPaths is null)
                {
                    string? fontName =
                        (string?) ConverterHelper.GetValueFromReader(ref reader, typeof(string), options);
                    FontSystem? outSystem =
                        fontName is not null
                            ? GlobalVars.GlobalContentManager.LoadFactory(new[] {fontName},
                                FontSystemFactory.Create(GlobalVars.GraphicsDevice), fontName)
                            : null;

                    //a little bit difficult to read here but all we are doing is that we are accounting for the
                    //possibility that we're given the name of an already loaded FontSystem rather than making a new
                    //FontSystem. We're basically checking if the given data is a valid string and corresponds to 
                    //an already loaded font.
                    if (fontName is not null && outSystem is not null)
                    {
                        return outSystem;
                    }

                    Debug.WriteLine($"Unable to parse array of fonts. Returning GlobalVars.DefaultFontSystem." +
                                    $"(FontSystemJsonConverter)");
                    return GlobalVars.DefaultFontSystem;
                }

                if (objPaths[0] is not string)
                {
                    Debug.WriteLine($"The first element of the directory array is not a string indicating the" +
                                    $"name. Returning GlobalVars.DefaultFontSystem (FontSystemJsonConverter)");
                    return GlobalVars.DefaultFontSystem;
                }

                //the first element of objPaths describes the NAME of the generated FontSystem
                List<string> paths = new();
                for (int i = 1; i < objPaths.Length; i++)
                {
                    if (objPaths[i] is string s) paths.Add(s);
                }

                string name = (string) objPaths[0]!;
                FontSystem? output = GlobalVars.GlobalContentManager.LoadFactory(paths.ToArray(),
                    FontSystemFactory.Create(GlobalVars.GraphicsDevice), name);
                if (output is null)
                {
                    Debug.WriteLine($"Error generating FontSystem of name {name}. Returning " +
                                    $"GlobalVars.DefaultFontSystem (FontSystemJsonConverter)");
                    return GlobalVars.DefaultFontSystem;
                }

                return output!;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Unexpected exception in Read of FontSystemJsonConverter. Returning " +
                                $"GlobalVars.DefaultFontSystem. Exception: {e}");
                return GlobalVars.DefaultFontSystem;
            }
        }

        /// <summary>
        /// SpriteFonts are not intended to be written as, therefore, this method will always return a
        /// NotImplementedException
        /// </summary>
        public override void Write(Utf8JsonWriter writer, FontSystem value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}