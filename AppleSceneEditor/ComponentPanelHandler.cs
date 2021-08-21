using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using AppleSceneEditor.Helpers;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;
using AssetManagementBase.Utility;
using FontStashSharp;
using GrappleFightNET5.Scenes;
using Myra.Graphics2D;
using Myra.Graphics2D.UI.Styles;
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

        /// <summary>
        /// The <see cref="JsonObject"/> instance whose values and components will be seen and changed.
        /// </summary>
        public JsonObject RootObject
        {
            get => _rootObject;
            set
            {
                _rootObject = value;
                
                //rebuild UI
                NameStackPanel.Widgets.Clear();
                ValueStackPanel.Widgets.Clear();
                
                //re-add the holder labels
                NameStackPanel.AddChild(new Label {Text = "Holder"});
                ValueStackPanel.AddChild(new Label {Text = "Holder"});

                BuildUI(0, value);
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
        /// <param name="rootObject">The <see cref="JsonObject"/> instance whose values and components will be seen and
        /// changed.</param>
        /// <param name="nameStackPanel">The <see cref="StackPanel"/> instance that holds the names of the components.
        /// </param>
        /// <param name="valueStackPanel">The <see cref="StackPanel"/> instance that holds the values of the components.
        /// </param>
        public ComponentPanelHandler(JsonObject rootObject, StackPanel nameStackPanel, StackPanel valueStackPanel)
        {
            (_rootObject, NameStackPanel, ValueStackPanel) = (rootObject, nameStackPanel, valueStackPanel);
            _font = AppleSerialization.Environment.DefaultFontSystem.GetFont(DefaultTextBoxFontSize);

            BuildUI(0, rootObject);
        }

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
                BuildUI(indentLevel + IndentationIncrement, obj);
            }
            
            //arrays
            foreach (JsonArray array in jsonObject.Arrays)
            {
                foreach (JsonObject obj in array)
                {
                    BuildUI(indentLevel + IndentationIncrement, obj);

                    //add blank label to separate entries in the array
                    NameStackPanel.AddChild(new Label {Text = ""});
                    ValueStackPanel.AddChild(new Label {Text = ""});
                }
            }
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
                Debug.WriteLine($"Value kind is not a string! Returning null. Value kind: {property.ValueKind}. " +
                                $" Method: ({nameof(CreateStringEditor)})");
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
            
            SpinButton spinButton = new(_font)
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
    }
}