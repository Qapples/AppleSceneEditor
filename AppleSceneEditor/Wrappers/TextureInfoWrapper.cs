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
    /// <see cref="IComponentWrapper"/> for <see cref="TextureInfo"/>.
    /// </summary>
    public class TextureInfoWrapper : IComponentWrapper
    {
        public JsonObject? JsonObject { get; set; }
        
        public Panel? UIPanel { get; set; }
        
        public bool IsEmpty { get; }
        
        public Type AssociatedType { get; } = typeof(TextureInfo);
        
        public JsonObject Prototype { get; }

        private TextureInfoWrapper(JsonObject jsonObject)
        {
            (JsonObject, IsEmpty) = (jsonObject, false);

            Prototype = new JsonObject();
            
            Prototype.Children.Add(new JsonObject("texturePath", Prototype, new List<JsonProperty>
            {
                new("path", "", Prototype, JsonValueKind.String),
                new("isContentPath", false, Prototype, JsonValueKind.False)
            }));
            
            Prototype.Children.Add(new JsonObject("texturePath", Prototype, new List<JsonProperty>
            {
                new("path", "", Prototype, JsonValueKind.String),
                new("isContentPath", false, Prototype, JsonValueKind.False)
            }));
            
            List<JsonProperty>? foundProperties =
                jsonObject.VerifyProperties(new[] {"path", "isContentPath"});
            
            if (foundProperties is null)
            {
                IsEmpty = true;
                return;
            }

            var (pathProp, isContentPathProp) = (foundProperties[0], foundProperties[1]);

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

            UIPanel = new Panel
                {Widgets = {ComponentWrapperExtensions.GenerateComponentGrid(widgetsPanel, this, "TextureInfo")}};
        }


        private TextureInfoWrapper()
        {
            (JsonObject, UIPanel, IsEmpty) = (null, null, true);
        }
    }
}