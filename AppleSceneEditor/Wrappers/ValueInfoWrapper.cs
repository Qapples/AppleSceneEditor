using System;
using System.Collections.Generic;
using AppleSceneEditor.Extensions;
using AppleSerialization.Info;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;

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
            
            UIPanel = new Panel
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
        }
    }
}