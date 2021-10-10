using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Wrappers;
using AppleSerialization;
using AppleSerialization.Json;
using Xunit;

namespace AppleSceneEditorTests
{
    public class WrapperTests
    {
        private static readonly string RootPath = Path.Combine("..", "..", "..", "..");
        private static readonly string TypeAliasPath = Path.Combine(RootPath, "AppleSceneEditor", "Config", "TypeAliases.txt");
        
        private const BindingFlags ActivatorFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        [Fact]
        public void AssociatedTypeTest()
        {
            foreach (Type i in ComponentWrapperExtensions.GetWrapperImplementers())
            {
                if (i == typeof(IComponentWrapper)) continue;
                
                Assert.True(
                    i.GetFields().FirstOrDefault(t =>
                        t.Name == "AssociatedType" && t.IsStatic && t.FieldType == typeof(Type)) != null,
                    $"type {i} does not have static readonly field of \"AssociatedType\"!");
            }
        }
        
        [Fact]
        public void ImplementersTest()
        {
            foreach (Type i in ComponentWrapperExtensions.GetWrapperImplementers())
            {
                if (i == typeof(IComponentWrapper)) continue;
                
                Assert.True(ComponentWrapperExtensions.Implementers.Values.Any(t => t == i),
                    $"type {i} that implements {nameof(IComponentWrapper)} does not add an entry to Implementers!");
            }
        }


        [Fact]
        public void PrototypeTest()
        {
            foreach (Type i in ComponentWrapperExtensions.GetWrapperImplementers())
            {
                if (i == typeof(IComponentWrapper)) continue;

                //were going to create a new instance and get it's associated type because we if use implementers in
                //component wrapper extensions then the results of this test will depend on others.
                Type? implementedType = null;

                Assert.True(
                    ComponentWrapperExtensions.Prototypes.Keys.Any(t =>
                        ComponentWrapperExtensions.Implementers.TryGetValue(t, out implementedType) &&
                        implementedType == i), (implementedType is not null
                        ? $"type {i} that implements {nameof(IComponentWrapper)} does not add an entry to Implementers! Cannot continue with test."
                        : $"type {i} does not add an entry to prototype!"));
            }
        }

        [Fact]
        public void TypePropertyTest()
        {
            foreach (var (type, prototype) in ComponentWrapperExtensions.Prototypes)
            {
                Assert.False(prototype.FindProperty("$type") is null, $"prototype of {type} does NOT " +
                                                                      $"have a $type property!");
            }
        }

        [Fact]
        public void AliasTest()
        {
            AppleSerialization.Environment.LoadTypeAliasFileContents(File.ReadAllText(TypeAliasPath));

            foreach (Type type in ComponentWrapperExtensions.Implementers.Keys)
            {
                if (type == typeof(IComponentWrapper)) continue;

                Assert.True(
                    AppleSerialization.Environment.TypeAliases.Values.Any(t =>
                        ConverterHelper.GetTypeFromString(t) == type), $"type {type} does not have an alias!");
            }
        }

        [Fact]
        public void ConstructorTest()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            const BindingFlags staticFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static |
                                             BindingFlags.FlattenHierarchy;
            
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
                bool hasStatic = wrapperType?.GetConstructor(staticFlags, null, Type.EmptyTypes, null) != null;

                Assert.False(!hasParam || !hasDefault || !hasStatic,
                    $"Type {wrapperType} does NOT have the required constructors!\n" +
                    $"{(!hasParam ? "Missing JsonObject constructor " : "")} " +
                    $"{(!hasDefault ? "Missing parameterless constructor" : "")}" +
                    $"{(!hasStatic ? "Missing static constructor" : "")}");
            }
        }
    }
}