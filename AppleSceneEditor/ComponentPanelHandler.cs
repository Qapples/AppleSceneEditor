using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Text.Json;
using AppleSceneEditor.Helpers;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;
using AssetManagementBase.Utility;
using FontStashSharp;
using GrappleFightNET5.Scenes;
using Myra.Graphics2D;
using Myra.Graphics2D.UI.Styles;
using StbImageSharp;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor
{
    /// <summary>
    /// Responsible for manipulating <see cref="StackPanel"/> instances so that they can be used to mainpulate the
    /// properties of entity components.
    /// </summary>
    public class ComponentPanelHandler
    {
        private const int MaxTextLength = 25;
        private const int DefaultTextBoxFontSize = 18;
        private const int IndentationIncrement = 8;

        private JsonObject _rootObject;

#nullable disable
        private Window _selectElementTypeWindow;
#nullable enable
        
        /// <summary>
        /// The <see cref="Desktop"/> instance where <see cref="NameStackPanel"/> and <see cref="ValueStackPanel"/>
        /// reside. Used to create new windows for selecting new element types.
        /// </summary>
        public Desktop Desktop { get; set; }
        
        /// <summary>
        /// The <see cref="JsonObject"/> instance whose values and components will be seen and changed.
        /// </summary>
        public JsonObject RootObject
        {
            get => _rootObject;
            set
            {
                _rootObject = value;
                
                RebuildUI();
            }
        }

        /// <summary>
        /// The <see cref="StackPanel"/> instance that holds the names of the components.
        /// </summary>
        public StackPanel NameStackPanel { get; set; }

        /// <summary>
        /// The <see cref="StackPanel"/> instance that holds the values of the components.
        /// </summary> 
        public StackPanel ValueStackPanel { get; set; }

        private readonly DynamicSpriteFont _font;

        /// <summary>
        /// Constructs an instance of <see cref="ComponentPanelHandler"/>.
        /// </summary>
        /// <param name="desktop">The <see cref="Desktop"/> instance where <see cref="NameStackPanel"/> and
        /// <see cref="ValueStackPanel"/> reside. Used to create new windows for selecting new element types.</param>
        /// <param name="rootObject">The <see cref="JsonObject"/> instance whose values and components will be seen and
        /// changed.</param>
        /// <param name="nameStackPanel">The <see cref="StackPanel"/> instance that holds the names of the components.
        /// </param>
        /// <param name="valueStackPanel">The <see cref="StackPanel"/> instance that holds the values of the components.
        /// </param>
        public ComponentPanelHandler(Desktop desktop, JsonObject rootObject, StackPanel nameStackPanel,
            StackPanel valueStackPanel)
        {
            (Desktop, _rootObject, NameStackPanel, ValueStackPanel) =
                (desktop, rootObject, nameStackPanel, valueStackPanel);
            _font = AppleSerialization.Environment.DefaultFontSystem.GetFont(DefaultTextBoxFontSize);

           _selectElementTypeWindow = new Window();
            BuildUI(0, rootObject);
        }
        
        //------------------
        // Public methods
        //------------------
        
        public bool SaveToScene(Scene scene)
        {
            if (scene.ScenePath is null)
            {
                Debug.WriteLine($"{nameof(SaveToScene)}: scene does not have a ScenePath! Cannot save to scene.");
                return false;
            }

            if (_rootObject.Name is null)
            {
                Debug.WriteLine($"{nameof(SaveToScene)}: the rootObject does not have a name! Cannot save to scene.");
                return false;
            }

            _rootObject.GenerateEntity(scene, scene.ScenePath is null
                ? null
                : Path.Combine(scene.ScenePath, "Entities", $"{_rootObject.Name}.entity"));

            return true;
        }
        
        /// <summary>
        /// Reconstructs and rebuilds any UI elements associated with this object.
        /// </summary>
        /// <param name="rootObject">The <see cref="JsonObject"/> to display. If not set to, then an internal root
        /// object will be used instead.</param>
        public void RebuildUI(JsonObject? rootObject = null)
        {
            rootObject ??= _rootObject;
            
            NameStackPanel.Widgets.Clear();
            ValueStackPanel.Widgets.Clear();

            //re-add the holder labels
            NameStackPanel.AddChild(new Label {Text = "Holder"});
            ValueStackPanel.AddChild(new Label {Text = "Holder"});

            BuildUI(0, rootObject);
        }
        
        //--------------------------
        // Private methods & more
        //--------------------------
        
        private void BuildUI(int indentLevel, JsonObject jsonObject)
        {
            //properties
            foreach (JsonProperty property in jsonObject.Properties)
            {
                AddProperty(indentLevel + IndentationIncrement, property);
            }

            //children
            foreach (JsonObject obj in jsonObject.Children)
            {
                AddINameStringEditor(obj, indentLevel);
                BuildUI(indentLevel + IndentationIncrement, obj);
            }

            //arrays
            foreach (JsonArray array in jsonObject.Arrays)
            {
                AddINameStringEditor(array, indentLevel);
                
                foreach (JsonObject obj in array)
                {
                    BuildUI(indentLevel + IndentationIncrement, obj);

                    //add these to separate entries in the array
                    NameStackPanel.AddChild(CreateAddElementButton(obj));
                    ValueStackPanel.AddChild(new Label());
                }
            }
        }

        private void AddINameStringEditor(IName name, int indentLevel)
        {
            TextBox? strEditor = CreateStringEditor(name);
            if (strEditor is not null)
            {
                strEditor.Margin = new Thickness(indentLevel + IndentationIncrement, 0, 0, 0);

                NameStackPanel.AddChild(strEditor);
                ValueStackPanel.AddChild(new Label());
            }
        }

        //----------------------------
        // Create UI element methods
        //----------------------------

        private TextButton CreateAddElementButton(JsonObject parentObj)
        {
            TextButton outButton = new()
            {
                Text = "+",
                Opacity = 0.5f,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Menu selectTypeMenu = CreateSelectElementTypeMenu(parentObj);

            outButton.Click += (o, e) =>
            {
                _selectElementTypeWindow.Content = selectTypeMenu;
                _selectElementTypeWindow.ShowModal(Desktop);
            };

            return outButton;
        }

        private Menu CreateSelectElementTypeMenu(JsonObject parentObj)
        {
            VerticalMenu menu = new()
            {
                Items =
                {
                    new MenuItem {Text = "Create property", Id = "propertyMenuItem"},
                    new MenuItem {Text = "Create array", Id = "arrayMenuItem"},
                    new MenuItem {Text = "Create child", Id = "childMenuItem"}
                }
            };

            //we're using a foreach loop here and a switch statement since the order of the elements in
            //_selectElementTypeMenu are subject to change
            foreach (IMenuItem iItem in menu.Items)
            {
                if (iItem is not MenuItem item) continue;

                JsonElementType newElemType;
                switch (item.Id)
                {
                    case "propertyMenuItem": newElemType = JsonElementType.Property; break;
                    case "arrayMenuItem": newElemType = JsonElementType.Array; break;
                    case "childMenuItem": newElemType = JsonElementType.Child; break;
                    default:
                        Debug.WriteLine($"{nameof(ComponentPanelHandler)}.{nameof(CreateSelectElementTypeMenu)}:" +
                                        "item id is not valid!. Must be \"propertyMenuItem\", \"arrayMenuItem\", or" +
                                        $"\"childMenuItem\". The actual id is: {item.Id}. Using JsonElementTypes." +
                                        "Property as a replacement value.");
                        newElemType = JsonElementType.Property;
                        break;
                }

                item.Selected += new NewElementTypeMethodHolder(parentObj, this, in newElemType).OutEvent;
                item.Selected += (o, e) => _selectElementTypeWindow.Close();
            }

            return menu;
        }

        private void AddProperty(int indentLevel, JsonProperty property)
        {
            TextBox? nameBox = CreateStringEditor(property, true);
            if (nameBox is not null)
            {
                nameBox.Margin = new Thickness(indentLevel, 0, 0, 0);
                NameStackPanel.AddChild(nameBox);
            }

            Widget? addWidget = null;
            switch (property.ValueKind)
            {
                case JsonValueKind.True or JsonValueKind.False:
                    addWidget = CreateBooleanEditor(property);
                    break;
                case JsonValueKind.String:
                    addWidget = CreateStringEditor(property);
                    break;
                case JsonValueKind.Number:
                    addWidget = CreateNumericEditor(property);
                    break;
            }

            addWidget ??= new Label {Text = "Not supported.", Font = _font};
            addWidget.Margin = new Thickness(indentLevel, 0, 0, 0);

            ValueStackPanel.AddChild(addWidget);
        }

        //---------------------------
        // Create editor methods
        //---------------------------

        private CheckBox? CreateBooleanEditor(JsonProperty property)
        {
            if (property.ValueKind is not JsonValueKind.False and not JsonValueKind.True)
            {
                Debug.WriteLine($"Value kind is not a boolean! Returning null. Value kind: {property.ValueKind}. " +
                                $" Method: ({nameof(CreateBooleanEditor)})");
                return null;
            }

            bool isChecked = (bool) (property.Value ?? false);
            CheckBox checkBox = new() {IsPressed = isChecked};

            checkBox.Click += (s, ea) => property.Value = checkBox.IsPressed;

            return checkBox;
        }

        private TextBox? CreateStringEditor(JsonProperty property, bool changeName = false)
        {
            if (property.ValueKind != JsonValueKind.String && !changeName)
            {
                Debug.WriteLine($"{nameof(ComponentPanelHandler)}.{nameof(CreateStringEditor)} (JsonProperty): " +
                                $"Value kind is not a string! Returning null. Value kind: {property.ValueKind}. ");
                return null;
            }

            TextBox textBox = new()
            {
                Text = changeName ? property.Name : property.Value as string,
                MaxWidth = 50,
                Font = _font
            };

            textBox.TextChanged += (s, ea) =>
            {
                if (textBox.Text is null || textBox.Text.Length > MaxTextLength) return;

                if (changeName) property.Name = textBox.Text;
                else property.Value = textBox.Text;
            };

            return textBox;
        }

        private TextBox? CreateStringEditor(IName name)
        {
            if (name.Name is null)
            {
                Debug.WriteLine($"{nameof(ComponentPanelHandler)}.{nameof(CreateStringEditor)} (IName):" +
                                $"parameter's name value is null! Cannot create string editor. Returning null.");
                return null;
            }

            TextBox textBox = new()
            {
                Text = name.Name,
                MaxWidth = 50,
                Font = _font
            };

            textBox.TextChanged += (s, e) =>
            {
                if (textBox.Text is null || textBox.Text.Length > MaxTextLength) return;

                name.Name = textBox.Text;
            };

            return textBox;
        }

        private SpinButton? CreateNumericEditor(JsonProperty property)
        {
            if (property.ValueKind != JsonValueKind.Number)
            {
                Debug.WriteLine($"Value kind is not a number! Returning null. Value kind: {property.ValueKind}. " +
                                $" Method: ({nameof(CreateNumericEditor)})");
                return null;
            }

            //property.Value should never be null under any circumstances but if it is set it to the default value of
            //an int which in this case is zero.
            property.Value ??= default(int);
            Type propertyValueType = property.Value.GetType();

            SpinButton spinButton = new()
            {
                Integer = propertyValueType.IsNumericInteger(),
                Nullable = propertyValueType.IsNullablePrimitive(),
                Value = property.Value != null
                    ? (float) Convert.ChangeType(property.Value, typeof(float))
                    : default(float?),
            };

            spinButton.ValueChanged += (s, a) =>
            {
                object? result = spinButton.Value != null
                    ? Convert.ChangeType(spinButton.Value.Value, propertyValueType)
                    : null;

                property.Value = result;
            };

            return spinButton;
        }

        //-----------------------------
        // New element handlers
        //-----------------------------

        private class NewElementTypeMethodHolder
        {
            public EventHandler OutEvent { get; init; }
            
            public ComponentPanelHandler Handler { get; set; } 
            public JsonObject Object { get; set; }

            private Window _initElementWindow;

            public NewElementTypeMethodHolder(JsonObject obj, ComponentPanelHandler handler,
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
                        Debug.WriteLine($"{nameof(NewElementTypeMethodHolder)}: for some reason, the type " +
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
        
        //------------------
        // Enums and misc.
        //------------------

        private enum JsonElementType
        {
            Property,
            Array,
            Child
        }

        private enum JsonPropertyType
        {
            Boolean,
            Integer,
            Float,
            String,
        }
    }
}