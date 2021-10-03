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
    public class ValueInfoWrapper : IComponentWrapper
    {
        public JsonObject? JsonObject { get; set; }
        
        public Panel? UIPanel { get; set; }

        public bool IsEmpty { get; }

        public static readonly Type AssociatedType = typeof(ValueInfo);

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
            JsonObject prototype = new();
            
            prototype.Properties.Add(new JsonProperty("$type", "ValueInfo", prototype, JsonValueKind.String));
            prototype.Properties.Add(new JsonProperty("valueType", "System.Int32", prototype, JsonValueKind.String));
            prototype.Properties.Add(new JsonProperty("value", "0", prototype, JsonValueKind.String));
            
            ComponentWrapperExtensions.Implementers.Add(typeof(ValueInfo), typeof(ValueInfoWrapper));
            ComponentWrapperExtensions.Prototypes.Add(typeof(ValueInfo), prototype);
        }
    }
}