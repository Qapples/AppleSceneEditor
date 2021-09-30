using System;
using System.Collections.Generic;
using AppleSceneEditor.Extensions;
using AppleSerialization.Json;
using GrappleFightNET5.Scenes.Info;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Wrappers
{
    /// <summary>
    /// <see cref="IComponentWrapper"/> for <see cref="TextureInfo"/>.
    /// </summary>
    public class TextureInfoWrapper : IComponentWrapper
    {
        public JsonObject? JsonObject { get; set; }
        
        public Panel? UIPanel { get; set; }
        
        public bool IsEmpty { get; }
        
        public Type AssociatedType { get; } = typeof(TextureInfo);

        private TextureInfoWrapper(JsonObject jsonObject)
        {
            (JsonObject, IsEmpty) = (jsonObject, false);

            List<JsonProperty>? foundProperties =
                jsonObject.VerifyProperties(new[] {"path", "isContentPath"});
            
            if (foundProperties is null)
            {
                IsEmpty = true;
                return;
            }

            var (pathProp, isContentPathProp) = (foundProperties[0], foundProperties[1]);

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
                                    new Label {Text = "texture path: "},
                                    ValueEditorFactory.CreateStringEditor(pathProp)
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
        
        
        private TextureInfoWrapper()
        {
            (JsonObject, UIPanel, IsEmpty) = (null, null, true);
        }
    }
}