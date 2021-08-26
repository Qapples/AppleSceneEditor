using System;
using System.Diagnostics;
using System.Text.Json;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor
{
    public partial class ComponentPanelHandler
    {
        private class NewElementHandler
        {
            public EventHandler OutEvent { get; init; }
            
            public ComponentPanelHandler Handler { get; set; } 
            public JsonObject Object { get; set; }

            private Window _initElementWindow;

            public NewElementHandler(JsonObject obj, ComponentPanelHandler handler,
                in JsonElementType type)
            {
                (Object, Handler) = (obj, handler);
                _initElementWindow = CreateInitElementWindow(type);

                switch (type)
                {
                    case JsonElementType.Property: OutEvent = CreateNewProperty; break;
                    case JsonElementType.Child: OutEvent = CreateNewChild; break;
                    case JsonElementType.Array: OutEvent = CreateNewArray; break;
                    default:
                        Debug.WriteLine($"{nameof(NewElementHandler)}: for some reason, the type " +
                                        "parameter value in the constructor for this type is not valid. The OutEvent " +
                                        "property will be set to the \"CreateNewProperty\" method.");
                        OutEvent = CreateNewProperty;
                        break;
                }
            }
            
            private void CreateNewProperty(object? sender, EventArgs? eventArgs)
            {
                _initElementWindow.ShowModal(Handler.Desktop);
                Handler.RebuildUI();
            }

            private void CreateNewArray(object? sender, EventArgs? eventArgs)
            {
                _initElementWindow.ShowModal(Handler.Desktop);
                Handler.RebuildUI();
            }

            private void CreateNewChild(object? sender, EventArgs? eventArgs)
            {
                _initElementWindow.ShowModal(Handler.Desktop);
                Handler.RebuildUI();
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

                stackPanel.AddChild(new Label {Text = "Enter the name of the element:", HorizontalAlignment = center});
                stackPanel.AddChild(nameTextBox);
                stackPanel.AddChild(new Label());

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

                        Object.Properties.Add(new JsonProperty(name, value, in kind));
                        Handler.RebuildUI();
                        break;
                    case JsonElementType.Array:
                        Object.Arrays.Add(new JsonArray(name) {new()});
                        Handler.RebuildUI();
                        break;
                    case JsonElementType.Child:
                        Object.Children.Add(new JsonObject(name));
                        Handler.RebuildUI();
                        break;
                }
            }
        }
    }
}