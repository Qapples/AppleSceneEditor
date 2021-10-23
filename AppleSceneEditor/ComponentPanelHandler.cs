using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Factories;
using AppleSceneEditor.Wrappers;
using AppleSerialization;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;
using AssetManagementBase.Utility;
using GrappleFightNET5.Scenes;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI.Styles;
using BindingFlags = System.Reflection.BindingFlags;
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
        
        public List<IComponentWrapper> Wrappers { get; set; }

        public ComponentPanelHandler(Desktop desktop, JsonObject rootObject, StackPanel propertyStackPanel)
        {
            Wrappers = new List<IComponentWrapper>();
            
            (Desktop, PropertyStackPanel, RootObject) = (desktop, propertyStackPanel, rootObject);
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
                // IComponentWrapper? wrapper = ComponentWrapperExtensions.CreateFromObject(jsonObj, Desktop);
                // if (wrapper is null) continue;
                //
                // PropertyStackPanel.AddChild(wrapper.UIPanel);

                Panel? widgets = CreateComponentWidgets(jsonObj, Desktop);
                if (widgets is null) continue;

                //GetHeader should not return null here since $type is verified in CreateComponentWidgets
                PropertyStackPanel.AddChild(CreateComponentGrid(jsonObj, widgets, GetHeader(jsonObj)!));
            }
        }
        
        private static string? GetHeader(JsonObject obj)
        {
            string? name = obj.FindProperty("$type")?.Value as string;

            return name;
        }

        private static Grid CreateComponentGrid(JsonObject obj, Panel widgetsPanel, string header)
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
                JsonArray? componentArray = obj.Parent?.FindArray("components");
                componentArray?.Remove(obj);
            };

            outGrid.AddChild(removeButton);

            return outGrid;
        }

        private static Panel? CreateComponentWidgets(JsonObject obj, Desktop desktop)
        {
            const string methodName = nameof(ComponentPanelHandler) + "." + nameof(CreateComponentWidgets);

            VerticalStackPanel stackPanel = new();

            JsonProperty? typeProp = obj.FindProperty("$type");
            if (typeProp?.Value is null || typeProp.ValueKind != JsonValueKind.String)
            {
                Debug.WriteLine($"{methodName}: can't find valid string $type property in JsonObject! Returning null.");
                return null;
            }

            Type? objType = ConverterHelper.GetTypeFromString((string) typeProp.Value);
            if (objType is null)
            {
                Debug.WriteLine($"{methodName}: can't find type of name ${typeProp.Value as string}. Returning null.");
                return null;
            }

            ParameterInfo[]? paramsInfo = GetConstructorParamTypes(objType);
            if (paramsInfo is null)
            {
                Debug.WriteLine($"{methodName}: can't find parameter with JsonConstructor attribute! Returning null.");
                return null;
            }

            foreach (var (property, type) in from property in obj.Properties
                from info in paramsInfo
                where property.Name == info.Name
                select (property, info.ParameterType))
            {
                switch (property.ValueKind)
                {
                    case JsonValueKind.Number:
                        stackPanel.AddChild(ValueEditorFactory.CreateNumericEditor(property));
                        break;
                    case JsonValueKind.False or JsonValueKind.True:
                        stackPanel.AddChild(ValueEditorFactory.CreateBooleanEditor(property));
                        break;
                    case JsonValueKind.String:
                        if (type == typeof(Vector2))
                        {
                            stackPanel.AddChild(
                                ValueEditorFactory.CreateVectorEditor(ValueEditorFactory.VectorType.Vector2, property));
                        }
                        else if (type == typeof(Vector3))
                        {
                            string[]? valueNames =
                                property.Name?.Equals("rotation", StringComparison.CurrentCultureIgnoreCase) == true
                                    ? new[] {"yaw", "pitch", "roll"}
                                    : null;
                            
                            stackPanel.AddChild(
                                ValueEditorFactory.CreateVectorEditor(ValueEditorFactory.VectorType.Vector3, property,
                                    valueNames));
                        }
                        else if (type == typeof(Vector4))
                        {
                            stackPanel.AddChild(
                                ValueEditorFactory.CreateVectorEditor(ValueEditorFactory.VectorType.Vector4, property));
                        }
                        
                        break;
                }
            }

            return new Panel {Widgets = {stackPanel}};
        }

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