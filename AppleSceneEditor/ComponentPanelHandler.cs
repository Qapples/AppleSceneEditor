using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppleSceneEditor.Commands;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Factories;
using AppleSerialization;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;
using GrappleFightNET5.Scenes;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI.Styles;
using JsonProperty = AppleSerialization.Json.JsonProperty;


//TODO: Overall: Reduce line count and verbosity. Feel like I can do a bit better here in terms of this.
namespace AppleSceneEditor
{
    /// <summary>
    /// Responsible for manipulating <see cref="StackPanel"/> instances so that they can be used to mainpulate the
    /// properties of entity components.
    /// </summary>
    public partial class ComponentPanelHandler
    {
        private const int MaxTextLength = 25;
        private const int DefaultTextBoxFontSize = 18;
        private const int IndentationIncrement = 8;
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

            _rootObject.GenerateEntity(scene, scene.ScenePath is null
                ? null
                : Path.Combine(scene.ScenePath, "Entities", $"{_rootObject.Name}.entity"));

            return true;
        }
        
        /// <summary>
        /// Reconstructs and rebuilds any UI elements associated with this object.
        /// </summary>
        /// <param name="rootObject">The <see cref="JsonObject"/> to display. If not set to, then an internal root
        /// object will be used instead.</param>
        public void RebuildUI(JsonObject? rootObject = null)
        {
            rootObject ??= _rootObject;

            PropertyStackPanel.Widgets.Clear();
            //re-add the holder labels so that the first actual elements are not hidden by the options bar.
            PropertyStackPanel.AddChild(new Label {Text = "Holder"});
            
            BuildUI();
        }
        
        //--------------------------
        // Private methods & more
        //--------------------------
        
        private void BuildUI()
        {
            foreach (JsonObject jsonObj in Components)
            {
                Panel? widgets = CreateComponentWidgets(jsonObj, Desktop);
                if (widgets is null) continue;

                //GetHeader should not return null here since $type is verified in CreateComponentWidgets
                //CreateComponentGrid is basically the drop down menu. widgets are the actual UI elements the user can
                //edit
                PropertyStackPanel.AddChild(CreateComponentGrid(jsonObj, widgets, _commands, GetHeader(jsonObj)!));
            }
        }
        
        private static string? GetHeader(JsonObject obj)
        {
            string? name = obj.FindProperty("$type")?.Value as string;

            return name;
        }

        private static Grid CreateComponentGrid(JsonObject obj, Panel widgetsPanel, CommandStream commands, string header)
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

            removeButton.Click += (_, _) => commands.AddCommandAndExecute(new RemoveComponentCommand(obj, outGrid));

            outGrid.AddChild(removeButton);

            return outGrid;
        }

        private static Panel? CreateComponentWidgets(JsonObject obj, Desktop desktop, Type? objectType = null)
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
            foreach (var (child, type) in from child in obj.Children
                from info in paramsInfo
                where child.Name == info.Name
                select (child, info.ParameterType))
            {
                stackPanel.AddChild(CreateComponentWidgets(child, desktop, type));
            }

            if (hasArray && (hasChild || hasProp)) stackPanel.AddChild(new Label());

            //arrays to stack panel
            //TODO: Add support for JsonArrays. Not many components use arrays, so not a huge issue for now.

            return new Panel {Widgets = {stackPanel}};
        }

        private static Widget? GenerateWidgetFromProperty(JsonProperty property, Type type)
        {
            const string methodName = nameof(ComponentPanelHandler) + "." + nameof(GenerateWidgetFromProperty);
            
            switch (property.ValueKind)
            {
                case JsonValueKind.Number:
                    return GenerateLabelAndEditor(property.Name, ValueEditorFactory.CreateNumericEditor(property));
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

                    return GenerateLabelAndEditor(property.Name, ValueEditorFactory.CreateStringEditor(property));
            }

            Debug.WriteLine($"{methodName}: property has invalid ValueKind! Must be either Number, False, True," +
                            $" or String! ValueKind is {property.ValueKind}");
            return null;
        }

        private static HorizontalStackPanel GenerateLabelAndEditor(string? label, Widget? editor) => new()
        {
            Widgets =
            {
                label is not null ? new Label {Text = $"{label}: "} : null,
                editor
            }
        };

        private static ParameterInfo[]? GetConstructorParamTypes(Type type) => (from ctor in type.GetConstructors()
            where ctor.GetCustomAttribute(typeof(JsonConstructorAttribute)) is not null
            select ctor).FirstOrDefault()?.GetParameters();
        
        public sealed class ComponentsNotFoundException : Exception
        {
            public ComponentsNotFoundException(string? entityId) : base(
                $"Cannot find component array in Entity with an id of {entityId ?? "(Entity has no id)"}!")
            {
            }
        }

        //------------------
        // Enums and misc.
        //------------------

        private enum JsonElementType
        {
            Property,
            Array,
            Object,
        }

        private enum JsonPropertyType
        {
            Boolean,
            Integer,
            Float,
            String,
        }
    }
}