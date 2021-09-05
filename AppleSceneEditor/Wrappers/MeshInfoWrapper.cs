using System;
using System.Globalization;
using AppleSerialization;
using AppleSerialization.Json;
using GrappleFightNET5.Scenes.Info;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Wrappers
{
    public class MeshInfoWrapper : IComponentWrapper
    {
        public JsonObject? JsonObject { get; set; }
        public Panel? UIPanel { get; set; }
        
        public bool IsEmpty { get; }

        public Type AssociatedType { get; } = typeof(MeshInfo);
        
        private MeshInfoWrapper(JsonObject jsonObject)
        {
            JsonObject = jsonObject;
            IsEmpty = false;

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
                                    new Label {Text = "Path: "}
                                }
                            },
                            new HorizontalStackPanel
                            {
                                Widgets =
                                {
                                    new Label {Text = "IsContentPath: "},
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

        public static MeshInfoWrapper? New(JsonObject jsonObject)
        {
            return null;
        }
    }
}