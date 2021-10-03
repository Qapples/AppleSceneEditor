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
    /// <summary>
    /// <see cref="IComponentWrapper"/> for <see cref="MeshInfo"/>.
    /// </summary>
    public class MeshInfoWrapper : IComponentWrapper
    {
        public JsonObject? JsonObject { get; set; }
        
        public Panel? UIPanel { get; set; }
        
        public bool IsEmpty { get; }

        public static readonly Type AssociatedType = typeof(MeshInfo);

        private MeshInfoWrapper(JsonObject jsonObject)
        {
            (JsonObject, IsEmpty) = (jsonObject, false);

            List<JsonProperty>? foundProperties =
                jsonObject.VerifyProperties(new[] {"meshIndex", "skinIndex", "path", "isContentPath"});

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
                {Widgets = {ComponentWrapperExtensions.GenerateComponentGrid(widgetsPanel, this, "MeshInfo")}};
        }
    

        private MeshInfoWrapper()
        {
            (JsonObject, UIPanel, IsEmpty) = (null, null, true);
        }

        static MeshInfoWrapper()
        {
            JsonObject prototype = new();
            
            prototype.Properties.Add(new JsonProperty("$type", "MeshInfo", prototype, JsonValueKind.String));
            prototype.Properties.Add(new JsonProperty("meshIndex", 0, prototype, JsonValueKind.Number));
            prototype.Properties.Add(new JsonProperty("skinIndex", 0, prototype, JsonValueKind.Number));

            prototype.Children.Add(new JsonObject("meshPath", prototype, new List<JsonProperty>
            {
                new("path", "", prototype, JsonValueKind.String),
                new("isContentPath", false, prototype, JsonValueKind.False)
            }));
            
            //ComponentWrapperExtensions.Implementers.Add(typeof(MeshInfo), typeof(MeshInfoWrapper));
            ComponentWrapperExtensions.Prototypes.Add(typeof(MeshInfo), prototype);
        }
    }
}