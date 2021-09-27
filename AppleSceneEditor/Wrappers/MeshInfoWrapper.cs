using System;
using System.Diagnostics;
using System.Globalization;
using AppleSceneEditor.Extensions;
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
            const StringComparison compare = StringComparison.CurrentCultureIgnoreCase;

            JsonObject = jsonObject;
            IsEmpty = false;

            JsonProperty? meshIndexProp = jsonObject.FindProperty("meshIndex", compare);
            JsonProperty? skinIndexProp = jsonObject.FindProperty("skinIndex", compare);
            JsonProperty? meshPathProp = jsonObject.FindProperty("path", compare);
            JsonProperty? isContentPathProp = jsonObject.FindProperty("isContentPath", compare);

            if (meshIndexProp is null || skinIndexProp is null || meshPathProp is null || isContentPathProp is null)
            {
                Debug.WriteLine($"{nameof(MeshInfoWrapper)} constructor: cannot find critical property! " +
                                "Missing properties:\n" +
                                (meshIndexProp is null ? "meshIndexProp cannot be found.\n" : "") +
                                (skinIndexProp is null ? "skinIndexProp cannot be found.\n" : "") +
                                (meshPathProp is null ? "MeshPathProp cannot be found.\n" : "") +
                                (isContentPathProp is null ? "isContentPathProp cannot be found." : ""));

                IsEmpty = true;
                return;
            }
            

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