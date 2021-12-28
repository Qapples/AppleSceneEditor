using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using AppleSerialization.Json;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.Extensions
{
    public static class IOHelper
    {
        public static Dictionary<string, JsonObject>? CreatePrototypesFromFile(string filePath)
        {
#if DEBUG
            const string methodName = nameof(IOHelper) + "." + nameof(CreatePrototypesFromFile);
#endif
            Utf8JsonReader reader = new(File.ReadAllBytes(filePath), new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
            
            JsonObject? rootObject = JsonObject.CreateFromJsonReader(ref reader);
            JsonArray? prototypes = rootObject?.FindArray("prototypes");

            if (prototypes is null)
            {
                Debug.WriteLine($"{methodName}: cannot find JsonArray with name \"prototypes\"! Returning null.");
                return null;
            }

            Dictionary<string, JsonObject> outDictionary = new();
            
            foreach (JsonObject obj in prototypes)
            {
                JsonProperty? typeProp = obj.FindProperty("$type");
                if (typeProp?.ValueKind != JsonValueKind.String) continue;

                //type should be string thanks to the check from above
                string type = (string) typeProp.Value!;
                
                outDictionary.Add(type, obj);
            }

            return outDictionary;
        }

        public static List<JsonObject> CreateJsonObjectsFromScene(string sceneDirectory)
        {
#if DEBUG
            const string methodName = nameof(IOHelper) + "." + nameof(CreateJsonObjectsFromScene);
#endif
            string entitiesFolderPath = Path.Combine(sceneDirectory, "Entities");

            if (!Directory.Exists(entitiesFolderPath))
            {
                Debug.WriteLine($"{methodName}: cannot find Entities folder in directory {sceneDirectory}! " +
                                "Returning a blank list.");
                return new List<JsonObject>();
            }

            List<JsonObject> outList = new();

            foreach (string entityPath in Directory.GetFiles(entitiesFolderPath))
            {
                Utf8JsonReader reader = new(File.ReadAllBytes(entityPath), new JsonReaderOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                JsonObject? newObj = JsonObject.CreateFromJsonReader(ref reader);
                if (newObj is not null) outList.Add(newObj);
            }

            return outList;
        }

        private const string BaseEntityContents = @"{
    ""components"": [
        {
        }
    ],
    ""id"" : ""Base""
}";

        //we're accepting a spritebatch instead of a graphics device since spriteBatch includes a reference to it's 
        //graphics device and we need both either way to create a new scene.
        public static bool CreateNewScene(string folderPath, SpriteBatch spriteBatch,
            int maxEntityCapacity = 128)
        {
#if DEBUG
            const string methodName = nameof(IOHelper) + "." + nameof(CreateNewScene);
#endif
            string worldPath = Path.Combine(folderPath, new DirectoryInfo(folderPath).Name + ".world");

            //create paths
            string entitiesPath = Path.Combine(folderPath, "Entities");
            Directory.CreateDirectory(Path.Combine(folderPath, "Systems"));
            Directory.CreateDirectory(Path.Combine(folderPath, "Entities"));
            Directory.CreateDirectory(Path.Combine(folderPath, "Content"));

            //create world file
            try
            {
                using (StreamWriter writer = File.CreateText(worldPath))
                {
                    writer.WriteLine($"WorldMaxCapacity: {maxEntityCapacity}");
                    writer.Flush();
                }

                //add base entity
                using (StreamWriter writer = File.CreateText(Path.Combine(entitiesPath, "BaseEntity")))
                {
                    writer.WriteLine(BaseEntityContents);
                    writer.Flush();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{methodName}: failed to create scene at {folderPath}. Exception:\n{e}");
                return false;
            }

            return true;
        }

        public static Dictionary<string, Image> GetFileIconsFromDirectory(string directory,
            GraphicsDevice graphicsDevice)
        {
#if DEBUG
            const string methodName = nameof(IOHelper) + "." + nameof(GetFileIconsFromDirectory);
#endif
            Dictionary<string, Image> outDict = new();

            foreach (string filePath in Directory.GetFiles(directory))
            {
                if (!IsImageExtension(Path.GetExtension(filePath)))
                {
                    Debug.WriteLine($"{methodName}: {filePath} does not have valid image extension. Ignoring.");
                    continue;
                }

                outDict[Path.GetFileName(filePath)] = new Image
                    {Renderable = new TextureRegion(Texture2D.FromFile(graphicsDevice, filePath))};
            }

            return outDict;
        }

        //supported formats: bmp, gif, jpg, png, tif and dds (only for simple textures).
        private static bool IsImageExtension(string extension) =>
            extension is ".bmp" or ".gif" or ".jpg" or ".png" or ".tif" or ".dds";
    }
}