using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppleSceneEditor.Commands;
using AppleSceneEditor.Exceptions;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Factories;
using AppleSerialization;
using AppleSerialization.Json;
using GrappleFight.Resource.Info.Interfaces;
using GrappleFight.Runtime;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.UI
{
    /// <summary>
    /// Responsible for manipulating <see cref="StackPanel"/> instances so that they can be used to mainpulate the
    /// properties of entity components.
    /// </summary>
    public class ComponentPanelHandler : IDisposable
    {
        private const string ComponentGridId = "ComponentGrid";

        private JsonObject _rootObject;
        
        public Desktop Desktop { get; set; }

        public JsonObject RootObject
        {
            get => _rootObject;
            set
            {
                _rootObject = value;
                
                JsonArray? components = _rootObject.FindArray("components");
                Components = components ?? throw new ComponentsNotFoundException(_rootObject.Name);

                RebuildUI();
            }
        }
        
        public JsonArray Components { get; private set; }
        
        public StackPanel PropertyStackPanel { get; set; }

        private CommandStream _commands;
        
        public ComponentPanelHandler(Desktop desktop, JsonObject rootObject, StackPanel propertyStackPanel,
            CommandStream commands)
        {
            //rootObject HAS to be set to last otherwise we will try building the UI when some fields have not
            //initalized yet.
            (Desktop, PropertyStackPanel, _commands, RootObject) = (desktop, propertyStackPanel, commands, rootObject);
        }
        
        //------------------
        // Public methods
        //------------------
        
        public bool SaveToScene(Scene scene)
        {
            if (scene.ScenePath is null)
            {
                Debug.WriteLine($"{nameof(SaveToScene)}: scene does not have a ScenePath! Cannot save to scene.");
                return false;
            }

            if (_rootObject.Name is null)
            {
                Debug.WriteLine($"{nameof(SaveToScene)}: the rootObject does not have a name! Cannot save to scene.");
                return false;
            }

            _rootObject.GenerateEntity(scene.World, scene.ScenePath is null
                ? null
                : Path.Combine(scene.ScenePath, "Entities", $"{_rootObject.Name}.entity"));
            
            scene.Compile();

            return true;
        }
        
        /// <summary>
        /// Reconstructs and rebuilds any UI elements associated with this object.
        /// </summary>
        public void RebuildUI()
        {
            PropertyStackPanel.Widgets.Clear();

            //re-add the holder labels so that the first actual elements are not hidden by the options bar.
            PropertyStackPanel.AddChild(new Label {Text = "Holder"});
            
            BuildUI();
        }

        public void Dispose()
        {
            (_rootObject, _commands, Desktop, Components, PropertyStackPanel) = (null!, null!, null!, null!, null!);
        }
        
        //--------------------------
        // Private methods & more
        //--------------------------
        
        private void BuildUI()
        {
            foreach (JsonObject jsonObj in Components)
            {
                Panel? widgets = CreateComponentWidgets(jsonObj, _commands);
                if (widgets is null) continue;

                //GetHeader should not return null here since $type is verified in CreateComponentWidgets
                //CreateComponentGrid is basically the drop down menu. widgets are the actual UI elements the user can
                //edit
                //a bit of a hack being done here in order to pass in the dropDown grid into the RemoveComponentCommand
                //constructor despite it not being instantiated
                Grid dropDown = null!;
                dropDown = CreateDropDown(widgets, GetHeader(jsonObj)!,
                    (_, _) => _commands.AddCommandAndExecute(new RemoveComponentCommand(jsonObj, dropDown)));
                PropertyStackPanel.AddChild(dropDown);
            }
        }

        private static Panel? CreateComponentWidgets(JsonObject obj, CommandStream commands, Type? objectType = null)
        {
            const string methodName = nameof(ComponentPanelHandler) + "." + nameof(CreateComponentWidgets);

            //if there is no objectType provided it can be safe to assume that there is no $type property in the object
            //which is useful to know later down when we place separators between elements (properties, children, arrays)
            bool typeProvided = objectType is not null;
            VerticalStackPanel stackPanel = new();

            //if a type is not specified for the object in the parameters, try to find it via the $type property.
            if (objectType is null)
            {
                JsonProperty? typeProp = obj.FindProperty("$type");
                if (typeProp?.Value is null || typeProp.ValueKind != JsonValueKind.String)
                {
                    Debug.WriteLine(
                        $"{methodName}: can't find valid string $type property in JsonObject! Returning null.");
                    return null;
                }

                objectType = ConverterHelper.GetTypeFromString((string) typeProp.Value);
                if (objectType is null)
                {
                    Debug.WriteLine(
                        $"{methodName}: can't find type of name ${typeProp.Value as string}. Returning null.");
                    return null;
                }
            }

            ParameterInfo[]? paramsInfo = GetConstructorParamTypes(objectType);
            if (paramsInfo is null)
            {
                Debug.WriteLine($"{methodName}: can't find parameter with JsonConstructor attribute! Returning null.");
                return null;
            }
            
            //hasProp is weird. $type is for the editor only for getting the type and we are not going to show it to
            //the user. these bools will be used to determine when to place a separator, and as to avoid placing a
            //separator after nothing, we must account for whenever or not we have $type.
            bool hasProp = obj.Properties.Count > (typeProvided ? 0 : 1);
            var (hasChild, hasArray) = (obj.Children.Count > 0, obj.Arrays.Count > 0);
            
            //properties to stack panel
            foreach (var (property, type) in from property in obj.Properties
                from info in paramsInfo
                where property.Name == info.Name
                select (property, info.ParameterType))
            {
                stackPanel.AddChild(GenerateWidgetFromProperty(property, type));
            }

            if (hasChild && hasProp) stackPanel.AddChild(new Label());

            //children/objects to stack panel
            foreach (var (child, t) in from child in obj.Children
                from info in paramsInfo
                where child.Name == info.Name
                select (child, info.ParameterType))
            {
                Type? childType = t.IsTypeJsonSerializable() ? t : ConverterHelper.GetTypeFromObject(child);

                stackPanel.AddChild(CreateComponentWidgets(child, commands, childType));
            }

            if (hasArray && (hasChild || hasProp)) stackPanel.AddChild(new Label());

            //arrays to stack panel
            foreach (JsonArray array in obj.Arrays)
            {
                VerticalStackPanel arrStackPanel = new();
                Grid arrDropDown = CreateDropDown(arrStackPanel, array.Name!);

                foreach (JsonObject arrObj in array)
                {
                    Panel? widgets = CreateComponentWidgets(arrObj, commands);
                    if (widgets is null) continue;

                    Grid dropDown = null!;
                    dropDown = CreateDropDown(widgets, GetHeader(arrObj)!,
                        (_, _) => commands.AddCommandAndExecute(
                            new RemoveArrayElementCommand(array, arrObj, dropDown)));

                    arrStackPanel.AddChild(dropDown);
                }

                arrStackPanel.AddChild(CreateAddArrayElementButton(array, arrStackPanel, commands));
                stackPanel.AddChild(arrDropDown);
            }

            return new Panel {Widgets = {stackPanel}};
        }

        private static Widget? GenerateWidgetFromProperty(JsonProperty property, Type type)
        {
            const string methodName = nameof(ComponentPanelHandler) + "." + nameof(GenerateWidgetFromProperty);
            
            switch (property.ValueKind)
            {
                case JsonValueKind.Number:
                    return GenerateLabelAndEditor(property.Name, ValueEditorFactory.CreateNumericEditor(property, type));
                case JsonValueKind.False or JsonValueKind.True:
                    return GenerateLabelAndEditor(property.Name, ValueEditorFactory.CreateBooleanEditor(property));
                case JsonValueKind.String:
                    if (type == typeof(Vector2))
                    {
                        return ValueEditorFactory.CreateVectorEditor(ValueEditorFactory.VectorType.Vector2, property);
                    }
                    else if (type == typeof(Vector3))
                    {
                        string[]? valueNames =
                            property.Name?.Equals("rotation", StringComparison.CurrentCultureIgnoreCase) == true
                                ? new[] {"yaw", "pitch", "roll"}
                                : null;

                        return ValueEditorFactory.CreateVectorEditor(ValueEditorFactory.VectorType.Vector3, property,
                            valueNames);
                    }
                    else if (type == typeof(Vector4))
                    {
                        return ValueEditorFactory.CreateVectorEditor(ValueEditorFactory.VectorType.Vector4, property);
                    }
                    else if (type == typeof(Rectangle))
                    {
                        return ValueEditorFactory.CreateVectorEditor(ValueEditorFactory.VectorType.Vector4, property,
                            new[] {"X", "Y", "Width", "Height"});
                    }

                    return GenerateLabelAndEditor(property.Name, ValueEditorFactory.CreateStringEditor(property));
            }

            Debug.WriteLine($"{methodName}: property has invalid ValueKind! Must be either Number, False, True," +
                            $" or String! ValueKind is {property.ValueKind}");
            return null;
        }

        private static Grid CreateDropDown<T>(T widgetsContrainer, string header, EventHandler? onRemoveClick = null) where T : Widget, IMultipleItemsContainer
        {
            Label headerLabel = new(null) {Text = header};
            headerLabel.ApplyLabelStyle(Stylesheet.Current.TreeStyle.LabelStyle);
           
            Grid dropDownGrid = MyraExtensions.CreateDropDown(widgetsContrainer, headerLabel, ComponentGridId);
            
            TextButton removeButton = new()
            {
                Text = "-",
                HorizontalAlignment = HorizontalAlignment.Right,
                GridColumn = 1
            };

            removeButton.Click += onRemoveClick;
            dropDownGrid.AddChild(removeButton);

            return dropDownGrid;
        }

        private static HorizontalStackPanel GenerateLabelAndEditor(string? label, Widget? editor) => new()
        {
            Widgets =
            {
                label is not null ? new Label {Text = $"{label}: "} : null,
                editor
            }
        };

        private static TextButton CreateAddArrayElementButton(JsonArray array, IMultipleItemsContainer arrayWidgets,
            CommandStream commands)
        {
#if DEBUG
            const string methodName = nameof(ComponentPanelHandler) + "." + nameof(CreateAddArrayElementButton);
#endif
            TextButton outButton = new()
            {
                Text = "Add"
            };

            outButton.Click += (_, _) =>
            {
                if (array.Count == 0)
                {
                    Debug.WriteLine($"{methodName} (button click): array is of length 0! Cannot create new " +
                                    "element. Try creating a new array altogether or changing the element(s) already there.");
                    return;
                }

                JsonObject newObj = (JsonObject) array[0].Clone();
                Panel? widgets = CreateComponentWidgets(newObj, commands);
                if (widgets is null) return;

                Grid dropDown = null!;
                dropDown = CreateDropDown(widgets, GetHeader(newObj)!,
                    (_, _) => commands.AddCommandAndExecute(new RemoveArrayElementCommand(array, newObj, dropDown)));

                commands.AddCommandAndExecute(new AddArrayElementCommand(array, arrayWidgets, newObj, dropDown));
            };

            return outButton;
        }

        private static string? GetHeader(JsonObject obj) => string.IsNullOrWhiteSpace(obj.Name)
            ? obj.FindProperty("$type")?.Value as string
            : obj.Name;

        private static ParameterInfo[]? GetConstructorParamTypes(Type type) => (from ctor in type.GetConstructors()
            where ctor.GetCustomAttribute(typeof(JsonConstructorAttribute)) is not null
            select ctor).FirstOrDefault()?.GetParameters();
    }
}