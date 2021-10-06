using System;
using System.Collections.Generic;
using System.Text.Json;
using AppleSceneEditor.Extensions;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.Wrappers
{
    public class ScriptInfoWrapper : IComponentWrapper
    {
        public JsonObject? JsonObject { get; set; }

        public Panel? UIPanel { get; set; }

        public bool IsEmpty { get; }

        public static readonly Type AssociatedType = typeof(GrappleFightNET5.Scenes.Info.ScriptInfo);


        private ScriptInfoWrapper(JsonObject jsonObject)
        {
            (JsonObject, IsEmpty) = (jsonObject, false);

            List<JsonProperty>? foundProperties = jsonObject.VerifyProperties(new[] {"ScriptFile"});

            if (foundProperties is null)
            {
                IsEmpty = true;
                return;
            }

            var (meshIndexProp, skinIndexProp, meshPathProp, isContentPathProp) = (foundProperties[0],
                foundProperties[1], foundProperties[2], foundProperties[3]);

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
                                    new Label {Text = "meshIndex: "},
                                    ValueEditorFactory.CreateNumericEditor(meshIndexProp)
                                }
                            },
                            new HorizontalStackPanel
                            {
                                Widgets =
                                {
                                    new Label {Text = "skinIndex: "},
                                    ValueEditorFactory.CreateNumericEditor(skinIndexProp)
                                }
                            },
                            new HorizontalStackPanel
                            {
                                Widgets =
                                {
                                    new Label {Text = "path: "},
                                    ValueEditorFactory.CreateStringEditor(meshPathProp)
                                }
                            },
                            new HorizontalStackPanel
                            {
                                Widgets =
                                {
                                    new Label {Text = "isContentPath: "},
                                    ValueEditorFactory.CreateBooleanEditor(isContentPathProp)
                                }
                            }
                        }
                    }
                }
            };

            UIPanel = new Panel
                {Widgets = {ComponentWrapperExtensions.GenerateComponentGrid(widgetsPanel, this, "ScriptInfo")}};
        }

        private ScriptInfoWrapper()
        {
            (JsonObject, UIPanel, IsEmpty) = (null, null, false);
        }

        static ScriptInfoWrapper()
        {
            JsonObject prototype = new();
            
            prototype.Properties.Add(new JsonProperty("$type", "ScriptInfo", prototype, JsonValueKind.String));
            prototype.Properties.Add(new JsonProperty(""));
            
            ComponentWrapperExtensions.Implementers.Add(AssociatedType, typeof(ScriptInfoWrapper));
            ComponentWrapperExtensions.Prototypes.Add(AssociatedType, prototype);
        }
    }
}