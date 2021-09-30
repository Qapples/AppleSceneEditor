using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using AppleSceneEditor.Extensions;
using AppleSerialization;
using AppleSerialization.Json;
using GrappleFightNET5.Scenes.Info;
using Myra.Graphics2D.UI;

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

        public Type AssociatedType { get; } = typeof(MeshInfo);

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
        }

        private MeshInfoWrapper()
        {
            (JsonObject, UIPanel, IsEmpty) = (null, null, true);
        }
    }
}