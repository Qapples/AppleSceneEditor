using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AppleSceneEditor;
using AppleSceneEditor.Wrappers;
using AppleSerialization.Json;
using Xunit;

namespace AppleSceneEditorTests
{
    public class WrapperTests
    {
        private static readonly string RootPath = Path.Combine("..", "..", "..", "..");
        private static readonly string TypeAliasPath = Path.Combine(RootPath, "AppleSceneEditor", "Config", "TypeAliases.txt");
        
        [Fact]
        public void PrototypeTest()
        { 
            AppleSerialization.Environment.LoadTypeAliasFileContents(File.ReadAllText(TypeAliasPath));
            MainGame.InitComponentPrototypes();
            
            foreach (string typeName in AppleSerialization.Environment.TypeAliases.Keys)
            {
                Assert.True(MainGame.NewComponentPrototypes.TryGetValue(typeName, out _),
                    $"{typeName} does NOT have a prototype!");
            }
        }

        [Fact]
        public void ConstructorTest()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            IEnumerable<Type> assemblyTypes;
            Type wrapperInterface = typeof(IComponentWrapper);

            //we're putting this here just in case we load assemblies that rely on other assemblies that we don't
            //reference
            try
            {
                assemblyTypes = wrapperInterface.Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                assemblyTypes = from type in e.Types
                    where type is not null
                    select (Type) type;
            }

            assemblyTypes = assemblyTypes.Where(wrapperInterface.IsAssignableFrom).ToList();

            foreach (Type wrapperType in assemblyTypes)
            {
                if (wrapperType == typeof(IComponentWrapper)) continue;

                bool hasParam = wrapperType?.GetConstructor(flags, null, new[] {typeof(JsonObject)}, null) != null;
                bool hasDefault = wrapperType?.GetConstructor(flags, null, Type.EmptyTypes, null) != null;

                Assert.False(!hasParam || !hasDefault,
                    $"Type {wrapperType} does NOT have the required constructors!\n" +
                    $"{(!hasParam ? "Missing JsonObject constructor " : "")} " +
                    $"{(!hasDefault ? "Missing parameterless constructor" : "")}");
            }
        }
    }
}