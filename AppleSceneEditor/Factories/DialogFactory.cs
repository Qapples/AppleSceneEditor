using System;
using System.Collections.Generic;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Factories
{
    public static class DialogFactory
    {
        public delegate void NewComponentOkClick(string typeName);
        
        public static Window CreateNewComponentDialog(IEnumerable<string> types, NewComponentOkClick onOkClick)
        {
            Panel panel = new();
            Window outWindow = new() {Content = panel};
            VerticalStackPanel stackPanel = new();

            ComboBox typeSelectionBox = new() {HorizontalAlignment = HorizontalAlignment.Center};
            foreach (string type in types)
            {
                typeSelectionBox.Items.Add(new ListItem {Text = type});
            }

            TextButton okButton = new() {Text = "OK", HorizontalAlignment = HorizontalAlignment.Right};
            TextButton cancelButton = new() {Text = "Cancel", HorizontalAlignment = HorizontalAlignment.Right};

            okButton.Click += (_, _) => onOkClick(typeSelectionBox.SelectedItem.Text);
            cancelButton.Click += (_, _) => outWindow.Close();

            stackPanel.AddChild(new Label
                {Text = "Select type of component", HorizontalAlignment = HorizontalAlignment.Center});
            stackPanel.AddChild(typeSelectionBox);
            stackPanel.AddChild(new HorizontalStackPanel
                {Widgets = {okButton, cancelButton}, HorizontalAlignment = HorizontalAlignment.Right});
            
            panel.Widgets.Add(stackPanel);

            return outWindow;
        }

        public static Window CreateAlreadyExistsDialog()
        {
            Panel panel = new();
            Window outWindow = new() {Content = panel};
            VerticalStackPanel stackPanel = new();

            stackPanel.AddChild(new Label
            {
                Text = "Cannot add component because another component of the same type already exists!",
                StyleName = "small",
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            
            TextButton okButton = new() {Text = "OK", HorizontalAlignment = HorizontalAlignment.Right};
            okButton.Click += (o, e) => outWindow.Close();

            stackPanel.AddChild(okButton);
            
            panel.Widgets.Add(stackPanel);
            
            return outWindow;
        }
    }
}