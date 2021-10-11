using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        
        public string Path { get; }

        public static readonly Type AssociatedType = typeof(GrappleFightNET5.Scenes.Info.ScriptInfo);

        private ScriptInfoWrapper(JsonObject jsonObject, Desktop desktop)
        {
            (JsonObject, IsEmpty, Path) = (jsonObject, false, "");

            List<JsonProperty>? foundProperties = jsonObject.VerifyProperties(new[] {"name"});

            if (foundProperties is null)
            {
                IsEmpty = true;
                return;
            }

            JsonProperty nameProp = foundProperties.First();
            TextBox? namePropEditor = ValueEditorFactory.CreateStringEditor(nameProp);

            var (dialogButton, pathBox, fileDialog) =
                ValueEditorFactory.CreateFileSelectionWidgets("", desktop, nameProp);
            
            fileDialog.Closed += (_, _) =>
            {
                string? fileName = System.IO.Path.GetFileName(nameProp.Value as string);
                fileName = fileName?.Remove(fileName.IndexOf('.'));
                if (fileName is null) return;
                
                nameProp.Value = System.IO.Path.GetFileName(nameProp.Value as string);

                if (namePropEditor is not null) namePropEditor.Text = nameProp.Value as string;
            };

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
                                    new Label {Text = "Name: "},
                                    namePropEditor
                                }
                            },
                            new HorizontalStackPanel
                            {
                                Spacing = 4,
                                Widgets =
                                {
                                    new Label {Text = "Script Path: "},
                                    pathBox,
                                    dialogButton,
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
            (JsonObject, UIPanel, IsEmpty, Path) = (null, null, false, "");
        }

        static ScriptInfoWrapper()
        {
            JsonObject prototype = new();
            
            prototype.Properties.Add(new JsonProperty("$type", "ScriptInfo", prototype, JsonValueKind.String));
            prototype.Properties.Add(new JsonProperty("name", "", prototype, JsonValueKind.String));
            
            ComponentWrapperExtensions.Implementers.Add(AssociatedType, typeof(ScriptInfoWrapper));
            ComponentWrapperExtensions.Prototypes.Add(AssociatedType, prototype);
        }
    }
}