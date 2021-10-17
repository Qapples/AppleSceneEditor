using System;
using System.Collections.Generic;
using System.Text.Json;
using AppleSceneEditor.Extensions;
using AppleSerialization.Json;
using GrappleFightNET5.Scenes.Info;
using Myra.Graphics2D.UI;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.Wrappers
{
    public class CollisionBoxWrapper : IComponentWrapper
    {
        public JsonObject? JsonObject { get; set; }
        
        public Panel? UIPanel { get; set; }
        
        public bool IsEmpty { get; }

        public static readonly Type AssociatedType = typeof(CollisionBoxInfo);

        private CollisionBoxWrapper(JsonObject jsonObject, Desktop desktop)
        {
            (JsonObject, IsEmpty) = (jsonObject, false);

            List<JsonProperty>? foundProperties =
                jsonObject.VerifyProperties(new[] {"position", "halfExtent", "rotation"});

            if (foundProperties is null)
            {
                IsEmpty = true;
                return;
            }

            var (positionProp, halfExtentProp, rotationProp) =
                (foundProperties[0], foundProperties[1], foundProperties[2]);

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
                                    new Label {Text = "position:"},
                                    ValueEditorFactory.CreateVector3Editor(positionProp),
                                }
                            },
                            new HorizontalStackPanel
                            {
                                Widgets =
                                {
                                    new Label {Text = "halfExtent:"},
                                    ValueEditorFactory.CreateVector3Editor(halfExtentProp),
                                }
                            },
                            new HorizontalStackPanel
                            {
                                Widgets =
                                {
                                    new Label {Text = "rotation:"},
                                    ValueEditorFactory.CreateVector3Editor(rotationProp),
                                }
                            },
                        }
                    }
                }
            };

            UIPanel = new Panel
            {
                Widgets = {ComponentWrapperExtensions.GenerateComponentGrid(widgetsPanel, this, "CollisionBoxInfo")}
            };
        }

        private CollisionBoxWrapper()
        {
            (JsonObject, UIPanel, IsEmpty) = (null, null, true);
        }

        static CollisionBoxWrapper()
        {
            JsonObject prototype = new();

            prototype.Properties.Add(new JsonProperty("$type", "CollisionBoxInfo", prototype, JsonValueKind.String));
            prototype.Properties.Add(new JsonProperty("position", "0 0 0", prototype, JsonValueKind.String));
            prototype.Properties.Add(new JsonProperty("halfExtent", "0 0 0", prototype, JsonValueKind.String));
            prototype.Properties.Add(new JsonProperty("rotation", "0 0 0", prototype, JsonValueKind.String));

            ComponentWrapperExtensions.Implementers.Add(AssociatedType, typeof(CollisionBoxWrapper));
            ComponentWrapperExtensions.Prototypes.Add(AssociatedType, prototype);
        }
    }
}