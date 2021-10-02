using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AppleSceneEditor.Wrappers;
using AppleSerialization;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

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
        public static Dictionary<Type, Type> Implementers { get; }

        private const BindingFlags ActivatorFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static Dictionary<Type, Type> LoadImplementers()
        {
            const string methodName = nameof(ComponentWrapperExtensions) + "." + nameof(LoadImplementers);
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
                if (type == typeof(IComponentWrapper)) continue;
                
                bool hasParam = type.GetConstructor(ActivatorFlags, null, new[] {typeof(JsonObject)}, null) != null;
                bool hasDefault = type.GetConstructor(ActivatorFlags, null, Type.EmptyTypes, null) != null;

                if (!hasParam || !hasDefault)
                {
                    Debug.WriteLine($"{methodName}: wrapper type ({type}) does not have the required constructors!\n" +
                                    $"{(!hasParam ? "missing JsonObject constructor " : "")} " +
                                    $"{(!hasDefault ? "missing parameterless constructor" : "")}");
                    continue;
                }

                //wrapper should NEVER be null.
                IComponentWrapper wrapper =
                    (IComponentWrapper) Activator.CreateInstance(type, ActivatorFlags, null, null, null)!;

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
        /// Creates a new object that implements <see cref="IComponentWrapper"/> with a given type specifying the type
        /// of data the given <see cref="JsonObject"/> is representing.
        /// </summary>
        /// <param name="jsonObject">The <see cref="JsonObject"/> instance that contains the data the wrapper will work
        /// with.</param>
        /// <param name="type">The <see cref="Type"/> that <see cref="jsonObject"/> has.</param>
        /// <returns>If there is a wrapper class associated with the specified type, then a new
        /// <see cref="IComponentWrapper"/> instance referencing that wrapper class is returned. Otherwise, OR if the
        /// returned wrapper instance has <see cref="IComponentWrapper.IsEmpty"/> set to true, then null is
        /// returned.</returns>
        /// <remarks>This method will attempt to create a <see cref="IComponentWrapper"/> instance regardless if the
        /// provided <see cref="JsonObject"/> has a type identifier that matches that of type parameter.</remarks>
        public static IComponentWrapper? CreateFromObject(JsonObject jsonObject, Type? type)
        {
            if (type is null) return null;

            const string methodName = nameof(ComponentWrapperExtensions) + "." + nameof(CreateFromObject);
            
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

            IComponentWrapper? outWrapper = Activator.CreateInstance(wrapperType, bindingAttr: ActivatorFlags,
                binder: null, args: new[] {jsonObject}, null) as IComponentWrapper;

            //outWrapper?.IsEmpty could be either true, false or null. Return null if it is true or null.
            return outWrapper?.IsEmpty != false ? null : outWrapper;
        }

        /// <summary>
        /// Creates a new object that implements <see cref="IComponentWrapper"/> with a given <see cref="JsonObject"/>.
        /// The type of the provided <see cref="JsonObject"/> will be attempt to found.
        /// </summary>
        /// <param name="jsonObject">The <see cref="JsonObject"/> instance that contains the data the wrapper will work
        /// with.</param>
        /// <returns>If a type was found in the provided <see cref="JsonObject"/> and if there is a wrapper
        /// class associated with that type, then a new <see cref="IComponentWrapper"/> instance referencing that
        /// wrapper class is returned. Otherwise, OR if the returned wrapper instance has
        /// <see cref="IComponentWrapper.IsEmpty"/> set to true, then null is returned.</returns>
        public static IComponentWrapper? CreateFromObject(JsonObject jsonObject)
        {
            JsonProperty? typeProp = jsonObject.FindProperty(AppleSerialization.Environment.TypeIdentifier);

            return typeProp?.Value is not string value
                ? null
                : CreateFromObject(jsonObject, ConverterHelper.GetTypeFromString(value));
        }

        /// <summary>
        /// Verifies that a <see cref="JsonObject"/> instance contains a collection JsonProperties with specified names.
        /// </summary>
        /// <param name="obj">The <see cref="JsonObject"/> to find the properties in.</param>
        /// <param name="propertyNames">The names of the properties to verify that the <see cref="JsonObject"/>.</param>
        /// <param name="printDebug">If set to true, then the names of properties that couldn't be find will be printed
        /// to the debug console. By default, set to false.</param>
        /// <param name="stringComparison">Determines how names should be found. By default, set to
        /// <see cref="StringComparison.CurrentCultureIgnoreCase"/></param>
        /// <returns>If every property was found, then a <see cref="List{T}"/> of JsonProperties that were found are
        /// returned. Otherwise, if a single property was not found, then null is returned.</returns>
        public static List<JsonProperty>? VerifyProperties(this JsonObject obj,
            IEnumerable<string> propertyNames, bool printDebug = true,
            in StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            const string methodName = nameof(ComponentWrapperExtensions) + "." + nameof(VerifyProperties);

            List<string> missingMessages = new();
            List<JsonProperty> outProperties = new();
            
            foreach (string name in propertyNames)
            {
                JsonProperty? property = obj.FindProperty(name, in stringComparison);

                if (property is null) missingMessages.Add($"{name} not found");
                else outProperties.Add(property);
            }

            if (missingMessages.Count == 0) return outProperties;

            if (printDebug)
            {
                Debug.WriteLine($"{methodName}: cannot find these properties: \n{string.Join(' ', missingMessages)}");
            }

            return null;
        }

        private const string ComponentGridId = "ComponentGrid";

        /// <summary>
        /// Creates a new <see cref="Grid"/> whose purpose is to hold <see cref="Widget"/> instances under a toggleable
        /// drop down.
        /// </summary>
        /// <param name="widgetsPanel">A panel containing <see cref="Widget"/> instances. These are the widgets that
        /// will be displayed when the drop down is toggled on.</param>
        /// <param name="wrapper">The <see cref="IComponentWrapper"/> <see cref="widgetsPanel"/> is apart of.</param>
        /// <param name="header">The header that is displayed next to the drop down button.</param>
        /// <returns>A <see cref="Grid"/> instance that represents a drop down button that shows the widgets in
        /// <see cref="widgetsPanel"/> when toggled on.</returns>
        public static Grid GenerateComponentGrid(Panel widgetsPanel, IComponentWrapper wrapper, string header)
        {
            widgetsPanel.GridRow = 1;
            widgetsPanel.GridColumn = 1;

            Grid outGrid = new()
            {
                ColumnSpacing = 4,
                RowSpacing = 4,
                Id = ComponentGridId
            };
            
            outGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            outGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
            outGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            outGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            
            ImageButton mark = new(null)
            {
                Toggleable = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            mark.PressedChanged += (_, _) =>
            {
                if (mark.IsPressed)
                {
                    outGrid.AddChild(widgetsPanel);
                }
                else
                {
                    outGrid.RemoveChild(widgetsPanel);
                }
            };
            
            mark.ApplyImageButtonStyle(Stylesheet.Current.TreeStyle.MarkStyle);
            outGrid.AddChild(mark);

            Label label = new(null)
            {
                Text = header,
                GridColumn = 1
            };
            
            label.ApplyLabelStyle(Stylesheet.Current.TreeStyle.LabelStyle);
            outGrid.AddChild(label);

            TextButton removeButton = new()
            {
                Text = "-",
                HorizontalAlignment = HorizontalAlignment.Right,
                GridColumn = 1
            };

            removeButton.Click += (_, _) =>
            {
                wrapper.UIPanel.RemoveFromParent();
                
                wrapper.JsonObject = null!;
                wrapper.UIPanel = null!;
                wrapper = null!;
            };

            outGrid.AddChild(removeButton);

            return outGrid;
        }

        static ComponentWrapperExtensions()
        {
            Implementers = LoadImplementers();
        }
    }
}