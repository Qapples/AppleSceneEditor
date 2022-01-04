using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Myra.Graphics2D.UI;
using SettingsPanelInitializer = AppleSceneEditor.Factories.SettingsPanelInitializers.SettingsPanelInitializer;

namespace AppleSceneEditor.Factories
{
    public static class DialogFactory
    {
        public delegate void NewComponentOkClick(string typeName);

        //---------------------------------
        // Public dialog creation methods
        //---------------------------------
        
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

            okButton.Click += (_, _) =>
            {
                onOkClick(typeSelectionBox.SelectedItem.Text);
                outWindow.Close();
            };
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

        public static Window CreateSetKeybindDialog(Dictionary<string, string> keybindDict, string keybindName)
        {
            Label currentKeybindLabel = new() {HorizontalAlignment = HorizontalAlignment.Center};
            TextButton cancelButton = new() {Text = "Cancel", Id = "CancelButton"};
            TextButton okButton = new() {Text = "OK", Id = "OkButton"};

            VerticalStackPanel panel = new()
            {
                Widgets =
                {
                    new Label
                    {
                        Text = "Press the keys you want to set the keybind to (press escape to clear):",
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    currentKeybindLabel,
                    new HorizontalStackPanel
                        {Widgets = {cancelButton, okButton}, HorizontalAlignment = HorizontalAlignment.Right}
                },
            };

            Window outWindow = new() {Content = panel};

            outWindow.KeyDown += (_, keys) => currentKeybindLabel.Text += keys.Data + " ";
            cancelButton.Click += (_, _) => outWindow.Close();
            okButton.Click += (_, _) =>
            {
                keybindDict[keybindName] = currentKeybindLabel.Text.Trim();
                GlobalFlag.SetFlag(GlobalFlags.KeybindUpdated, true);
                outWindow.Close();
            };

            return outWindow;
        }

        public static Window CreateSettingsDialogFromFile(Desktop desktop, string filePath, string configDirectory)
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

            //add functionality for switching panels via the menu and initialize menus.
            Menu panelSelectionMenu = (Menu) root.FindWidgetById("PanelSelectionMenu");
            foreach (var menuItem in panelSelectionMenu.Items)
            {
                MenuItem item = (MenuItem) menuItem;
                if (!item.UserData.TryGetValue("_PanelId", out var panelName))
                {
                    Debug.WriteLine($"{methodName} (item selected): PanelSelectionMenu item with id " +
                                    $"{item.Id} does not have a \"_PanelID\" entry! Ignoring.");
                    continue;
                }

                SettingsPanelInitializer? initializer = GetSettingsPanelInitializer(panelName);
                if (initializer is null)
                {
                    Debug.WriteLine($"{methodName} (item selected): panel of name {panelName} does NOT have an" +
                                    " initializer! Ignoring.");
                    continue;
                }

                Panel selectedPanel = (Panel) mainPanel.FindWidgetById(panelName);
                initializer(selectedPanel, desktop, configDirectory);
                item.Selected += (_, _) => UpdatePanelVisibility(mainPanel, selectedPanel);
            }
            
            //when the window closes set every panel to disabled and invisible as a way of signifying that the window
            //has closed.
            outWindow.Closed += (window, _) =>
            {
                //the window parameter represents outWindow as an object, and the outWindow's content is a stack panel.
                StackPanel panel = (StackPanel) ((Window) window).Content;

                foreach (Widget widget in panel.Widgets)
                {
                    if (widget is Panel p)
                    {
                        (p.Visible, p.Enabled) = (false, false);
                    }
                }
            };

            return outWindow;
        }
        
        //-----------------
        // Private methods
        //-----------------

        //There's probably a better name to be had with this method, but I can't come up with one. So, docs are going
        //to have to do for now.
        /// <summary>
        /// Within a <see cref="IMultipleItemsContainer"/>, make all <see cref="Panel"/> instances invisible except
        /// for one specified panel.
        /// </summary>
        /// <remarks>This method performs a REFERENCE comparison, not an ID comparison!</remarks>
        /// <param name="panelsContainer"><see cref="IMultipleItemsContainer"/> with the <see cref="Panel"/> instances to make
        /// visibile/invisible</param>
        /// <param name="panel">The <see cref="Panel"/> to make visible while the other panels are invisible</param>
        private static void UpdatePanelVisibility(IMultipleItemsContainer panelsContainer, Panel panel)
        {
            (panel.Visible, panel.Enabled) = (true, true);
            
            foreach (Widget widget in panelsContainer.Widgets)
            {
                if (widget is Panel p && p != panel)
                {
                    (p.Visible, p.Enabled) = (false, false);
                }
            }
        }

        private static SettingsPanelInitializer? GetSettingsPanelInitializer(string panelName) => panelName switch
        {
            "KeybindPanel" => SettingsPanelInitializers.KeybindsPanelInitializer,
            _ => null
        };
    }
}