using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AppleSceneEditor.Wrappers;
using AppleSerialization;
using AppleSerialization.Json;

namespace AppleSceneEditor.Extensions
{
    /// <summary>
    /// Assists in working with instances that implement <see cref="IComponentWrapper"/>.
    /// </summary>
    public static class ComponentWrapperExtensions
    {
        /// <summary>
        /// All classes that implement <see cref="IComponentWrapper"/>. The key a type and the value is the wrapper
        /// associated with that type (if there is one)
        /// </summary>
        private static Dictionary<Type, Type> Implementers { get; }

        private const BindingFlags ActivatorFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        

        private static Dictionary<Type, Type> LoadImplementers()
        {
            string methodName = $"{nameof(ComponentWrapperExtensions)}.{nameof(LoadImplementers)}";
            Dictionary<Type, Type> outDictionary = new();
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

            foreach (Type type in assemblyTypes)
            {
                //wrapper should NOT Be null.
                if (type == typeof(IComponentWrapper) ||
                    Activator.CreateInstance(type, ActivatorFlags, null, null, null) is not IComponentWrapper wrapper)
                {
                    continue;
                }

                if (outDictionary.ContainsKey(wrapper.AssociatedType))
                {
                    Debug.WriteLine($"{methodName}: cannot include {type} because another type has already " +
                                    $"implemented {wrapper.AssociatedType}");
                    continue;
                }
                
                outDictionary.Add(wrapper.AssociatedType, type);
            }

            return outDictionary;
        }

        /// <summary>
        /// Creates a new object that implements <see cref="IComponentWrapper"/> with a specified type that it wraps
        /// around.
        /// </summary>
        /// <param name="jsonObject">The <see cref="JsonObject"/> instance that contains the data the wrapper will work
        /// with.</param>
        /// <param name="type">The <see cref="Type"/> that <see cref="jsonObject"/> has.</param>
        /// <returns>If there is a wrapper class associated with the specified type (<see cref="type"/>), then a new
        /// <see cref="IComponentWrapper"/> instance referencing that wrapper class is returned. Otherwise, null is
        /// returned.</returns>
        public static IComponentWrapper? CreateFromType(JsonObject jsonObject, Type? type)
        {
            if (type is null) return null;

            string methodName = $"{nameof(ComponentWrapperExtensions)}.{nameof(CreateFromType)}";
            
            if (!Implementers.TryGetValue(type, out var wrapperType))
            {
                Debug.WriteLine($"{methodName}: type ({type}) does not have a wrapper!");
                return null;
            }

            if (!typeof(IComponentWrapper).IsAssignableFrom(wrapperType))
            {
                //this should NOT happen.
                Debug.WriteLine($"{methodName}: {type}'s associated wrapper ({wrapperType}) does not implement " +
                                $"{nameof(IComponentWrapper)}");

                return null;
            }

            return Activator.CreateInstance(wrapperType, bindingAttr: ActivatorFlags, binder: null,
                args: new[] {jsonObject}, null) as IComponentWrapper;
        }

        /// <summary>
        /// Creates a new object that implements <see cref="IComponentWrapper"/> with a specified type (in string form)
        /// that it wraps around.
        /// </summary>
        /// <param name="jsonObject">The <see cref="JsonObject"/> instance that contains the data the wrapper will work
        /// with.</param>
        /// <param name="typeName">The name of the type that <see cref="jsonObject"/> has.</param>
        /// <returns>If <see cref="typeName"/> is valid and if there is a wrapper class associated with that type, then
        /// a new <see cref="IComponentWrapper"/> instance referencing that wrapper class is returned. Otherwise, null
        /// is returned.</returns>
        public static IComponentWrapper? CreateFromType(JsonObject jsonObject, string typeName) =>
            CreateFromType(jsonObject, ConverterHelper.GetTypeFromString(typeName));

        static ComponentWrapperExtensions()
        {
            Implementers = LoadImplementers();
        }
    }
}