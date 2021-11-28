using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AppleSceneEditor.Commands;
using AppleSerialization.Json;
using Xunit;

namespace AppleSceneEditorTests
{
    public class CommandTests
    {
        private Dictionary<Type, object> _defaultObjects = new()
        {
        };
        
        [Fact]
        public void DisposeTest()
        {
#if DEBUG
            const string methodInfo = nameof(CommandTests) + "." + nameof(DisposeTest);
#endif
            List<Type> commandTypes = GetImplementers(typeof(IEditorCommand));

            foreach (Type type in commandTypes)
            {
                if (type == typeof(IEditorCommand)) continue;

                //verify that each constructor set's dispose to false
                IEditorCommand? instance = null;
                foreach (ConstructorInfo constructor in type.GetConstructors())
                {
                    //most constructors aren't equipped to handle null cases so try and create blank objects
                    ParameterInfo[] paramInfos = constructor.GetParameters();
                    object?[] paramVals = new object[paramInfos.Length];

                    for (int i = 0; i < paramVals.Length; i++)
                    {
                        //resort to passing in null as a param if the object does not have a default constructor or does
                        //not have an associated object in the _defaultObjects dictionary.

                        Type paramType = paramInfos[i].ParameterType;
                        ConstructorInfo? defaultCtor = paramType.GetConstructor(Array.Empty<Type>());

                        if (defaultCtor is not null)
                        {
                            paramVals[i] = defaultCtor.Invoke(null);
                        }
                        else if (_defaultObjects.TryGetValue(paramType, out var obj))
                        {
                            paramVals[i] = obj;
                        }
                        else
                        {
                            Debug.WriteLine($"{methodInfo} (WARNING): type ({paramType}) in constructor " +
                                            $"({constructor}) does NOT have a default constructor and/or does not have " +
                                            $"an object in {nameof(_defaultObjects)} and therefore will be passed into " +
                                            "the constructor as null!");
                            paramVals[i] = null;
                        }
                    }
                    
                    instance = (IEditorCommand) constructor.Invoke(paramVals);
                    
                    Assert.True(!instance.Disposed, $"{constructor} does not set disposed to false!");
                }

                Assert.True(instance is not null, $"{type} does not have a constructor!");

                instance!.Dispose();
                
                Assert.True(instance!.Disposed, $"{instance} does not set disposed to true after disposing!");
            }
        }

        private List<Type> GetImplementers(Type baseType)
        {
            IEnumerable<Type> assemblyTypes;

            //we're putting this here just in case we load assemblies that rely on other assemblies that we don't
            //reference
            try
            {
                assemblyTypes = baseType.Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                assemblyTypes = from type in e.Types
                    where type is not null
                    select (Type) type;
            }
        
            return assemblyTypes.Where(baseType.IsAssignableFrom).ToList();
        }
    }
}