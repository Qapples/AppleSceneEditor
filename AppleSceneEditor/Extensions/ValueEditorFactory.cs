using System;
using System.Diagnostics;
using System.Text.Json;
using AppleSerialization.Json;
using AssetManagementBase.Utility;
using FontStashSharp;
using Myra.Graphics2D.UI;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.Extensions
{
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
                MaxWidth = 50,
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
                MaxWidth = 50
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
    }
}