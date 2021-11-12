using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using AppleSerialization.Json;
using GrappleFightNET5.Scenes;
using Microsoft.Xna.Framework.Graphics;
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
        public static Scene CreateNewScene(string folderPath, SpriteBatch spriteBatch,
            int maxEntityCapacity = 128)
        {
            string worldPath = Path.Combine(folderPath, new DirectoryInfo(folderPath).Name + ".world");

            //create paths
            string entitiesPath = Path.Combine(folderPath, "Entities");
            Directory.CreateDirectory(Path.Combine(folderPath, "Systems"));
            Directory.CreateDirectory(Path.Combine(folderPath, "Entities"));
            Directory.CreateDirectory(Path.Combine(folderPath, "Content"));

            //create world file
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

            return new Scene(folderPath, spriteBatch.GraphicsDevice, null, spriteBatch, true);
        }
    }
}