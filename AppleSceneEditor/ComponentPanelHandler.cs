using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Wrappers;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;
using AssetManagementBase.Utility;
using GrappleFightNET5.Scenes;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using JsonProperty = AppleSerialization.Json.JsonProperty;


//TODO: Overall: Reduce line count and verbosity. Feel like I can do a bit better here in terms of this.
namespace AppleSceneEditor
{
    /// <summary>
    /// Responsible for manipulating <see cref="StackPanel"/> instances so that they can be used to mainpulate the
    /// properties of entity components.
    /// </summary>
    public partial class ComponentPanelHandler
    {
        private const int MaxTextLength = 25;
        private const int DefaultTextBoxFontSize = 18;
        private const int IndentationIncrement = 8;

        private JsonObject _rootObject;

#nullable disable
        private Window _selectElemTypeWindow;
        private Window _selectArrElemTypeWindow;
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

            (_selectElemTypeWindow, _selectArrElemTypeWindow) = (new Window(), new Window());
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
                    IComponentWrapper? wrapper = ComponentWrapperExtensions.CreateFromObject(obj);

                    BuildUI(indentLevel + IndentationIncrement, obj);

                    //add these to separate entries in the array
                    NameStackPanel.AddChild(CreateAddElemButton(obj));
                    ValueStackPanel.AddChild(new Label());
                }
            }
        }

        private void AddINameStringEditor(IName name, int indentLevel)
        {
            TextBox? strEditor = ValueEditorFactory.CreateStringEditor(name);
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
        //TODO: Find a way to reduce code reuse here. The AddElem and AddArrElem methods use almost the exact same logic with different variables swapped around.

        private TextButton CreateAddElemButton(JsonObject parentObj)
        {
            TextButton outButton = new()
            {
                Text = "+",
                Opacity = 0.5f,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Menu selectTypeMenu = CreateSelectElemTypeMenu(parentObj);

            outButton.Click += (o, e) =>
            {
                _selectElemTypeWindow.Content = selectTypeMenu;
                _selectElemTypeWindow.ShowModal(Desktop);
            };

            return outButton;
        }

        public TextButton CreateAddArrElemButton(JsonArray array)
        {
            TextButton outButton = new()
            {
                Text = "+",
                Opacity = 0.5f,
                HorizontalAlignment = HorizontalAlignment.Right,
                TextColor = Color.Red
            };
            
            Menu selectTypeMenu = CreateSelectArrElemTypeMenu(array);

            outButton.Click += (o, e) =>
            {
                _selectArrElemTypeWindow.Content = selectTypeMenu;
                _selectArrElemTypeWindow.ShowModal(Desktop);
            };

            return outButton;
        }

        private Menu CreateSelectElemTypeMenu(JsonObject parentObj)
        {
            VerticalMenu outMenu = new()
            {
                Items =
                {
                    new MenuItem {Text = "Create property", Id = "propertyMenuItem"},
                    new MenuItem {Text = "Create array", Id = "arrayMenuItem"},
                    new MenuItem {Text = "Create child", Id = "childMenuItem"}
                }
            };

            //we're using a foreach loop here and a switch statement since the order of the elements in
            //_selectElemTypeMenu are subject to change
            foreach (IMenuItem iItem in outMenu.Items)
            {
                if (iItem is not MenuItem item) continue;

                JsonElementType newElemType;
                switch (item.Id)
                {
                    case "propertyMenuItem": newElemType = JsonElementType.Property; break;
                    case "arrayMenuItem": newElemType = JsonElementType.Array; break;
                    case "childMenuItem": newElemType = JsonElementType.Object; break;
                    default:
                        Debug.WriteLine($"{nameof(ComponentPanelHandler)}.{nameof(CreateSelectElemTypeMenu)}:" +
                                        "item id is not valid!. Must be \"propertyMenuItem\", \"arrayMenuItem\", or" +
                                        $"\"childMenuItem\". The actual id is: {item.Id}. Using JsonElementTypes." +
                                        "Property as a replacement value.");
                        newElemType = JsonElementType.Property;
                        break;
                }

                item.Selected += new NewElementHandler(parentObj, this, in newElemType).OutEvent;
                item.Selected += (o, e) => _selectElemTypeWindow.Close();
            }

            return outMenu;
        }

        private Menu CreateSelectArrElemTypeMenu(JsonArray array)
        {
            VerticalMenu outMenu = new()
            {
                Items =
                {
                    new MenuItem {Text = "Create value", Id = "valueMenuItem"},
                    new MenuItem {Text = "Create object", Id = "objectMenuItem"}
                }
            };
        
            //we're using a foreach loop here and a switch statement since the order of the elements in
            //_selectElemTypeMenu are subject to change
            foreach (IMenuItem iItem in outMenu.Items)
            {
                if (iItem is not MenuItem item) continue;
        
                JsonElementType newElemType;
                switch (item.Id)
                {
                    case "valueMenuItem": newElemType = JsonElementType.Property; break;
                    case "objectMenuItem": newElemType = JsonElementType.Object; break;
                    default:
                        Debug.WriteLine($"{nameof(ComponentPanelHandler)}.{nameof(CreateSelectArrElemTypeMenu)}:" +
                                        "item id is not valid!. Must be \"valueMenuItem\" or \"objectMenuItem\" " +
                                        $". The actual id is: {item.Id}. Using JsonElementType.Property as a " +
                                        "replacement value.");
                        newElemType = JsonElementType.Property;
                        break;
                }
        
                item.Selected += new NewElementHandler(array, this, in newElemType).OutEvent;
                item.Selected += (o, e) => _selectArrElemTypeWindow.Close();
            }
        
            return outMenu;
        }

        private void AddProperty(int indentLevel, JsonProperty property)
        {
            TextBox? nameBox = ValueEditorFactory.CreateStringEditor(property, true);
            if (nameBox is not null)
            {
                nameBox.Margin = new Thickness(indentLevel, 0, 0, 0);
                NameStackPanel.AddChild(nameBox);
            }

            Widget? addWidget = null;
            switch (property.ValueKind)
            {
                case JsonValueKind.True or JsonValueKind.False:
                    addWidget = ValueEditorFactory.CreateBooleanEditor(property);
                    break;
                case JsonValueKind.String:
                    addWidget = ValueEditorFactory.CreateStringEditor(property);
                    break;
                case JsonValueKind.Number:
                    addWidget = ValueEditorFactory.CreateNumericEditor(property);
                    break;
            }

            addWidget ??= new Label {Text = "Not supported."};
            addWidget.Margin = new Thickness(indentLevel, 0, 0, 0);

            ValueStackPanel.AddChild(addWidget);
        }

        //------------------
        // Enums and misc.
        //------------------

        private enum JsonElementType
        {
            Property,
            Array,
            Object,
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