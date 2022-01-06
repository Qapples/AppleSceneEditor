using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using AppleSceneEditor.UI;
using AppleSerialization;
using AppleSerialization.Json;
using AssetManagementBase.Utility;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using Myra.Utility;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.Factories
{
    //TODO: Add docs. Not super important but do it at some point.
    public static class ValueEditorFactory
    {
        private const int MaxTextLength = 25;
        
        //-----------------
        // PUBLIC METHODS
        //-----------------
     
        public static CheckBox? CreateBooleanEditor(JsonProperty property)
        {
            if (property.ValueKind is not JsonValueKind.False and not JsonValueKind.True)
            {
                Debug.WriteLine($"Value kind is not a boolean! Returning null. Value kind: {property.ValueKind}. " +
                                $" Method: ({nameof(CreateBooleanEditor)})");
                return null;
            }

            bool isChecked = (bool) (property.Value ?? false);
            CheckBox checkBox = new() {IsPressed = isChecked};

            checkBox.Click += (s, ea) =>
            {
                property.Value = checkBox.IsPressed;
                property.ValueKind = checkBox.IsPressed ? JsonValueKind.True : JsonValueKind.False;
            };

            return checkBox;
        }

        public static TextBox? CreateStringEditor(JsonProperty property, bool changeName = false)
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
                StyleName = "small"
            };

            textBox.TextChanged += (s, ea) =>
            {
                if (textBox.Text is null || textBox.Text.Length > MaxTextLength) return;

                if (changeName) property.Name = textBox.Text;
                else property.Value = textBox.Text;
            };

            return textBox;
        }

        public static TextBox? CreateStringEditor(IName name)
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
            };

            textBox.TextChanged += (s, e) =>
            {
                if (textBox.Text is null || textBox.Text.Length > MaxTextLength) return;

                name.Name = textBox.Text;
            };

            return textBox;
        }

        public static SpinButton? CreateNumericEditor(JsonProperty property)
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

        public static (TextButton dialogButton, TextBox pathBox, FileDialog fileDialog) CreateFileSelectionWidgets(
            string filter, Desktop desktop, JsonProperty? property = null)
        {
            TextBox pathBox = new()
            {
                Readonly = true,
                HintText = "File Path",
                HintTextEnabled = true
            };

            FileDialog fileDialog = new(FileDialogMode.OpenFile) {Filter = filter};

            fileDialog.Closed += (_, _) =>
            {
                if (!fileDialog.Result) return;

                string filePath = fileDialog.FilePath;
                if (string.IsNullOrEmpty(filePath)) return;

                pathBox.Text = filePath;

                if (property?.ValueKind == JsonValueKind.String)
                {
                    property.Value = filePath;
                }
            };

            TextButton dialogButton = new()
            {
                Text = "Select File..."
            };
            dialogButton.Click += (_, _) => fileDialog.ShowModal(desktop);

            return (dialogButton, pathBox, fileDialog);
        }

        public static HorizontalStackPanel? CreateVectorEditor(in VectorType vectorType, JsonProperty property,
            IList<string>? valueNames = null)
        {
            const string methodName = nameof(ValueEditorFactory) + "." + nameof(CreateVectorEditor);

            //init valueNames
            int vectorCount = (int) vectorType;

            //Set valueNames to X, Y, Z, or W if is not set to (depends on the vectorType)
            if (valueNames is null)
            {
                valueNames = new string[vectorCount];
                (valueNames[0], valueNames[1]) = ("X", "Y");

                if (vectorCount > 2) valueNames[2] = "Z";
                if (vectorCount > 3) valueNames[3] = "W";
            }

            //check for valid values
            if (valueNames.Count < vectorCount)
            {
                Debug.WriteLine($"{methodName}: the amount of provided value names is less than it should be! " +
                                $"valueNames has a count of {valueNames.Count} when it should be atleast {vectorCount}. " +
                                "Returning null.");
                return null;
            }

            if (property.ValueKind != JsonValueKind.String || property.Value is not string propertyValue)
            {
                Debug.WriteLine($"{methodName}: property's value is NOT a string! Returning null.");
                return null;
            }

            Span<float> values = stackalloc float[vectorCount];
            if (!ParseHelper.TryParseVector(propertyValue, ref values))
            {
                Debug.WriteLine($"{methodName}: can't parse property as space seperated vector float values! " +
                                $"Returning null.");
                return null;
            }
            
            //this method changes the data in the JsonProperty according to the data the user enters in the UI.
            void ValueChangingMethod(object? boxObj, ValueChangingEventArgs<string> args, JsonProperty tempProperty,
                TextBox[] otherBoxes)
            {
                if (boxObj is not TextBox box) return;
                
                if (string.IsNullOrWhiteSpace(args.NewValue))
                {
                    args.NewValue = "0";
                    box.CursorPosition = 1;
                    box.SelectAll();
                    
                    return;
                }
                
                if (!float.TryParse(args.NewValue, out _))
                {
                    args.Cancel = true;
                    return;
                }
            }
            
            Label[] labels = valueNames.Select(s => new Label {Text = $"{s}: "}).ToArray();
            TextBox[] boxes = new TextBox[vectorCount];
            HorizontalStackPanel outStackPanel = new() {Widgets = {new Label {Text = $"{property.Name}: "}}};

            //add UI elements to the outgoing stack panel
            for (int i = 0; i < vectorCount; i++)
            {
                boxes[i] = new TextBox {Text = values[i].ToString()};
                boxes[i].ValueChanging += (o, args) => ValueChangingMethod(o, args, property, boxes);
                boxes[i].TextChanged += (_, _) => UpdateJsonVectorProperty(property, boxes);

                //ensure that there is a space between each value
                if (i > 0)
                {
                    labels[i].Text = " " + labels[i].Text;
                }

                outStackPanel.AddChild(labels[i]);
                outStackPanel.AddChild(boxes[i]);
            }

            return outStackPanel;
        }
        
        //-------------------
        // PRIVATE METHODS
        //-------------------

        private static void UpdateJsonVectorProperty(JsonProperty property, TextBox[] boxes)
        {
            StringBuilder valueBuilder = new();
            foreach (TextBox otherBox in boxes) valueBuilder.Append(otherBox.Text + " ");
            valueBuilder.Remove(valueBuilder.Length - 1, 1);
            property.Value = valueBuilder.ToString();
        }
        
        
        //-------
        // MISC
        //-------

        public enum VectorType
        {
            Vector2 = 2,
            Vector3 = 3,
            Vector4 = 4
        }
    }
}