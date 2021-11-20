using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public static Window CreateSettingsDialogFromFile(string filePath)
        {
#if DEBUG
            const string methodName = nameof(DialogFactory) + "." + nameof(CreateAlreadyExistsDialog);
#endif
            //we're hard casting and using exception-vulnerable functions here because the settings file being valid is
            //vital for the editor to run property and any error is to be known immediately.
            
            Project project = Project.LoadFromXml(File.ReadAllText(filePath));
            Widget root = project.Root;
            StackPanel mainPanel = (StackPanel) root.FindWidgetById("MainPanel");
            Window outWindow = new() {Content = mainPanel, MinWidth = 600, MinHeight = 600};

            foreach (Widget widget in mainPanel.Widgets)
            {
                if (widget is Panel)
                {
                    (widget.Visible, widget.Enabled) = (false, false);
                }
            }

            //add functionality for switching panels via the menu.
            Menu panelSelectionMenu = (Menu) root.FindWidgetById("PanelSelectionMenu");
            foreach (var menuItem in panelSelectionMenu.Items)
            {
                MenuItem item = (MenuItem) menuItem;

                item.Selected += (_, _) =>
                {
                    if (!item.UserData.TryGetValue("_PanelId", out var panelName))
                    {
                        Debug.WriteLine($"{methodName} (item selected): PanelSelectionMenu item with id " +
                                        $"{item.Id} does not have a \"_PanelID\" entry! Ignoring.");
                        return;
                    }
                    
                    UpdatePanelVisibility(mainPanel, (Panel) mainPanel.FindWidgetById(panelName));
                };
            }

            return outWindow;
        }

        //There's probably a better name to be had with this method, but I can't come up with one. So, docs are going
        //to have to do for now.
        /// <summary>
        /// Within a <see cref="IMultipleItemsContainer"/>, make all <see cref="Panel"/> instances invisible except
        /// for one specified panel.
        /// </summary>
        /// <remarks>This method performs a REFERENCE comparison, not an ID comparison!</remarks>
        /// <param name="panels"><see cref="IMultipleItemsContainer"/> with the <see cref="Panel"/> instances to make
        /// visibile/invisible</param>
        /// <param name="panel">The <see cref="Panel"/> to make visible while the other panels are invisible</param>
        private static void UpdatePanelVisibility(IMultipleItemsContainer panels, Panel panel)
        {
            (panel.Visible, panel.Enabled) = (true, true);
            
            foreach (Widget widget in panels.Widgets)
            {
                if (widget is Panel p && p != panel)
                {
                    (p.Visible, p.Enabled) = (false, false);
                }
            }
        }
    }
}