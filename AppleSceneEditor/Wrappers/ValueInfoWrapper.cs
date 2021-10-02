using System;
using System.Collections.Generic;
using System.Text.Json;
using AppleSceneEditor.Extensions;
using AppleSerialization.Info;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.Wrappers
{
    /// <summary>
    /// <see cref="IComponentWrapper"/> for <see cref="ValueInfoWrapper"/>
    /// </summary>
    public class ValueInfoWrapper : JsonPrototype, IComponentWrapper
    {
        public JsonObject? JsonObject { get; set; }
        
        public Panel? UIPanel { get; set; }

        public bool IsEmpty { get; }

        public Type AssociatedType { get; } = typeof(ValueInfo);

        private ValueInfoWrapper(JsonObject jsonObject)
        {
            (JsonObject, IsEmpty) = (jsonObject, false);

            List<JsonProperty>? foundProperties = jsonObject.VerifyProperties(new[] {"valueType", "value"});

            if (foundProperties is null)
            {
                IsEmpty = true;
                return;
            }

            var (valueTypeProp, valueProp) = (foundProperties[0], foundProperties[1]);
            
            Panel widgetsPanel = new()
            {
                Widgets =
                {
                    new VerticalStackPanel
                    {
                        Widgets =
                        {
                            new HorizontalStackPanel
                            {
                                Widgets =
                                {
                                    new Label {Text = "valueType:"},
                                    ValueEditorFactory.CreateStringEditor(valueTypeProp)
                                }
                            },
                            new HorizontalStackPanel
                            {
                                Widgets =
                                {
                                    new Label {Text = "value:"},
                                    ValueEditorFactory.CreateStringEditor(valueProp)
                                }
                            }
                        }
                    }
                }
            };

            UIPanel = new Panel
                {Widgets = {ComponentWrapperExtensions.GenerateComponentGrid(widgetsPanel, this, "ValueInfo")}};
        }

        private ValueInfoWrapper()
        {
            (JsonObject, UIPanel, IsEmpty) = (null, null, true);
        }
        
        static ValueInfoWrapper()
        {
            Prototype = new JsonObject();
            
            Prototype.Properties.Add(new JsonProperty("$type", "ValueInfo", Prototype, JsonValueKind.String));
            Prototype.Properties.Add(new JsonProperty("valueType", "System.Int32", Prototype, JsonValueKind.String));
            Prototype.Properties.Add(new JsonProperty("value", "0", Prototype, JsonValueKind.String));
        }
    }
}