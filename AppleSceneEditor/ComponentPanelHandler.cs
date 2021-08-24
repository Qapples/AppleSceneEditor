using System;
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

        private JsonObject _currentBuildedObject;

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

            _selectElementTypeWindow = CreateSelectElementTypeWindow();
            BuildUI(0, rootObject);
        }

        private void BuildUI(int indentLevel, JsonObject jsonObject)
        {
            _currentBuildedObject = jsonObject;

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

                    //add these to separate entries in the array
                    NameStackPanel.AddChild(CreateAddElementButton());
                    ValueStackPanel.AddChild(new Label {Text = ""});
                }
            }
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

        private TextButton CreateAddElementButton()
        {
            TextButton outButton = new()
            {
                Text = "+",
                Opacity = 0.5f,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            outButton.Click += (o, e) => _selectElementTypeWindow.ShowModal(Desktop);

            return outButton;
        }

        private Window CreateSelectElementTypeWindow()
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

            Window outWindow = new() {Content = menu};

            //we're using a foreach loop here and a switch statement since the order of the elements in
            //_selectElementTypeMenu are subject to change
            foreach (IMenuItem iItem in menu.Items)
            {
                if (iItem is not MenuItem item) continue;

                switch (item.Id)
                {
                    case "propertyMenuItem": item.Selected += CreateNewProperty; break;
                    case "arrayMenuItem": item.Selected += CreateNewArray; break;
                    case "childMenuItem": item.Selected += CreateNewChild; break;
                }
            }

            return outWindow;
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
        // Element type menu methods
        //-----------------------------

        private void CreateNewProperty(object? sender, EventArgs? eventArgs)
        {
            _currentBuildedObject.Properties.Add(new JsonProperty());
            RebuildUI();
        }

        private void CreateNewArray(object? sender, EventArgs? eventArgs)
        {
            _currentBuildedObject.Arrays.Add(new JsonArray());
            RebuildUI();
        }

        private void CreateNewChild(object? sender, EventArgs? eventArgs)
        {
            _currentBuildedObject.Children.Add(new JsonObject());
            RebuildUI();
        }

        private record IndentedTextButton(TextButton Button, int IndentLevel);
    }
}