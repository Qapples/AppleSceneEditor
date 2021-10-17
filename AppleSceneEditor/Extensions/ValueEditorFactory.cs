using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Json;
using AppleSerialization;
using AppleSerialization.Json;
using AssetManagementBase.Utility;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using Myra.Utility;
using JsonProperty = AppleSerialization.Json.JsonProperty;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace AppleSceneEditor.Extensions
{
    //TODO: Add docs. Not super important but do it at some point.
    public static class ValueEditorFactory
    {
        private const int MaxTextLength = 25;
     
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

            checkBox.Click += (s, ea) => property.Value = checkBox.IsPressed;

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

        public static HorizontalStackPanel? CreateVector3Editor(JsonProperty property)
        {
            const string methodName = nameof(ValueEditorFactory) + "." + nameof(CreateVector3Editor);
            
            var (xLabel, yLabel, zLabel) =
                (new Label {Text = " X: "}, new Label {Text = " Y: "}, new Label {Text = " Z: "});
            var (xBox, yBox, zBox) = (new TextBox(), new TextBox(), new TextBox());

            if (property.ValueKind != JsonValueKind.String || property.Value is not string propertyValue)
            {
                Debug.WriteLine($"{methodName}: property's value is NOT a string! Returning null.");
                return null;
            }

            if (!ParseHelper.TryParseVector3(propertyValue, out Vector3 value))
            {
                Debug.WriteLine($"{methodName}: cannot parse property as vector3! Returning null.");
                return null;
            }

            (xBox.Text, yBox.Text, zBox.Text) = (value.X.ToString(), value.Y.ToString(), value.Z.ToString());

            void TextChangeMethod(object? boxObj, ValueChangedEventArgs<string> args, JsonProperty tempProperty)
            {
                if (boxObj is not TextBox box) return;

                if (!float.TryParse(args.NewValue, out _))
                {
                    box.Text = args.OldValue;
                    return;
                }

                if (string.IsNullOrEmpty(args.NewValue)) box.Text = "0";

                tempProperty.Value = $"{xBox.Text} {yBox.Text} {zBox.Text}";
            }

            xBox.TextChanged += (o, a) => TextChangeMethod(o, a, property);
            yBox.TextChanged += (o, a) => TextChangeMethod(o, a, property);
            zBox.TextChanged += (o, a) => TextChangeMethod(o, a, property);

            return new HorizontalStackPanel
            {
                Widgets =
                {
                    xLabel, xBox,
                    yLabel, yBox,
                    zLabel, zBox
                }
            };
        }
    }
}