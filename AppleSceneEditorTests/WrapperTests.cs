using System;
using System.IO;
using AppleSceneEditor;
using Xunit;
using Xunit.Sdk;

namespace AppleSceneEditorTests
{
    public class WrapperTests
    {
        private static readonly string RootPath = Path.Combine("..", "..", "..", "..");
        
        [Fact]
        public void PrototypeTest()
        { 
            string typeAliasFile = Path.Combine(RootPath, "AppleSceneEditor", "Config", "TypeAliases.txt");
            
            AppleSerialization.Environment.LoadTypeAliasFileContents(File.ReadAllText(typeAliasFile));
            foreach (string typeName in AppleSerialization.Environment.TypeAliases.Keys)
            {
                Assert.True(MainGame.NewComponentPrototypes.TryGetValue(typeName, out _),
                    $"{typeName} does NOT have a prototype!");
            }
        }
    }
}