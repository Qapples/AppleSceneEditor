using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor
{
    public partial class ComponentPanelHandler
    {
        //TODO: Change this into it's own dialog instead of being just a handler?
        private class NewElementHandler 
        {
            public EventHandler OutEvent { get; init; }
            
            public ComponentPanelHandler PanelHandler { get; set; } 
            
            public JsonObject? Object { get; set; }
            
            public JsonArray? Array { get; set; }

            public bool IsHandlingObject { get; set; }
            
            private Window _initElementWindow;

            public NewElementHandler(JsonObject obj, ComponentPanelHandler panelHandler,
                in JsonElementType type)
            {
                (Object, PanelHandler, IsHandlingObject) = (obj, panelHandler, true);
                _initElementWindow = CreateInitElementWindow(type);

                OutEvent = (o, e) =>
                {
                    _initElementWindow.ShowModal(PanelHandler.Desktop);
                    PanelHandler.RebuildUI();
                };
            }

            public NewElementHandler(JsonArray array, ComponentPanelHandler panelHandler, in JsonElementType type)
            {
                (Array, PanelHandler, IsHandlingObject) = (array, panelHandler, false);
                _initElementWindow = CreateInitElementWindow(type);

                OutEvent = (o, e) =>
                {
                    _initElementWindow.ShowModal(PanelHandler.Desktop);
                    PanelHandler.RebuildUI();
                };
            }

            
            private Window CreateInitElementWindow(JsonElementType elementType)
            {
                const HorizontalAlignment center = HorizontalAlignment.Center;
                Window outWindow = new();
                
                VerticalStackPanel stackPanel = new() {HorizontalAlignment = center};

                TextBox nameTextBox = new() {Text = "Enter name here...", HorizontalAlignment = center};
                TextButton finishButton = new() {Text = "Finish", HorizontalAlignment = center};
                ComboBox typeComboBox = new()
                {
                    Items =
                    {
                        new ListItem {Text = "Boolean", Id = "boolean"},
                        new ListItem {Text = "Integer", Id = "integer"},
                        new ListItem {Text = "Float", Id = "float"},
                        new ListItem {Text = "String", Id = "string"}
                    },
                    SelectedIndex = 0,
                    HorizontalAlignment = center
                };
                typeComboBox.SelectedItem = typeComboBox.Items[0];

                finishButton.Click += (o, e) =>
                {
                    FinishButtonClick(nameTextBox.Text, in elementType, typeComboBox.SelectedItem.Id switch
                    {
                        "boolean" => JsonPropertyType.Boolean,
                        "integer" => JsonPropertyType.Integer,
                        "float" => JsonPropertyType.Float,
                        "string" => JsonPropertyType.String,
                        _ => JsonPropertyType.Integer,
                    });
                    
                    outWindow.Close();
                };

                if (IsHandlingObject)
                {
                    stackPanel.AddChild(new Label
                        {Text = "Enter the name of the element:", HorizontalAlignment = center});
                    stackPanel.AddChild(nameTextBox);
                    stackPanel.AddChild(new Label());
                }

                if (elementType == JsonElementType.Property)
                {
                    stackPanel.AddChild(new Label {Text = "Select type of the element:", HorizontalAlignment = center});
                    stackPanel.AddChild(typeComboBox);
                    stackPanel.AddChild(new Label());
                }

                stackPanel.AddChild(finishButton);

                outWindow.Content = stackPanel;

                return outWindow;
            }

            private void FinishButtonClick(string name, in JsonElementType elementType,
                in JsonPropertyType propertyType)
            {
                switch (elementType)
                {
                    case JsonElementType.Property:
                        object value = default(int);
                        JsonValueKind kind = JsonValueKind.Number;
                            
                        switch (propertyType)
                        {
                            case JsonPropertyType.Boolean:
                                value = default(bool);
                                kind = (bool)value ? JsonValueKind.True : JsonValueKind.False;
                                break;
                            case JsonPropertyType.Integer:
                                value = default(int);
                                kind = JsonValueKind.Number;
                                break;
                            case JsonPropertyType.Float:
                                value = default(float);
                                kind = JsonValueKind.Number;
                                break;
                            case JsonPropertyType.String:
                                value = "";
                                kind = JsonValueKind.String;
                                break;
                        }

                        if (IsHandlingObject) Object.Properties.Add(new JsonProperty(name, value, in kind));
                        else Array.Add(new JsonProperty(null, value, in kind));
                        PanelHandler.RebuildUI();
                        break;
                    case JsonElementType.Array:
                        Object.Arrays.Add(new JsonArray(name) {new()});
                        PanelHandler.RebuildUI();
                        break;
                    case JsonElementType.Object:
                        Object.Children.Add(new JsonObject(name));
                        PanelHandler.RebuildUI();
                        break;
                }
            }
        }
    }
}