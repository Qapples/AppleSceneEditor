using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Factories
{
    public static class SettingsPanelInitializers
    {
        public delegate void SettingsPanelInitializer(Panel panel, Desktop desktop, string configDirectory);

        //Again, we want an exception to be thrown if we can't find any of the UI widgets we need to initialize
        //because it's very important to know ASAP if something vital is missing!

        public static void KeybindsPanelInitializer(Panel panel, Desktop desktop, string configDirectory)
        {
            const HorizontalAlignment horCenter = HorizontalAlignment.Center;

            //Required elements
            StackPanel nameStack = (StackPanel) panel.FindWidgetById("KeybindNameStack");
            StackPanel valueStack = (StackPanel) panel.FindWidgetById("KeybindValueStack");
            string keybindsPath = Path.Combine(configDirectory, "Keybinds.txt");

            //Allow the panel to change focus.
            panel.AcceptsKeyboardFocus = true;

            using StreamReader reader = new(keybindsPath);

            string? line;
            bool heldRegion = false;
            Dictionary<string, string> nameKeybindDict = new();

            while ((line = reader.ReadLine()) is not null)
            {
                if (line is "#HELD" or "#NOTHELD")
                {
                    heldRegion = line == "#HELD";
                    nameKeybindDict[line] = "";
                    continue;
                }

                //keybinds are in the form of:   name: keybinds
                //keybinds can have multiple keys separated by spaces
                int colonIndex = line.IndexOf(':');
                string name = line[..colonIndex];
                string keybind = line[(colonIndex + 2)..].Trim();

                nameKeybindDict[name] = keybind;

                //Add buttons
                nameStack.AddChild(new Label {Text = name, HorizontalAlignment = horCenter});

                TextButton valueButton = new() {Text = keybind, HorizontalAlignment = horCenter};
                valueButton.Click += (_, _) =>
                {
                    Window dialog = DialogFactory.CreateSetKeybindDialog(nameKeybindDict, name);
                    dialog.Closed += (_, _) => valueButton.Text = nameKeybindDict[name];
                    
                    dialog.ShowModal(desktop);
                };
                
                valueStack.AddChild(valueButton);
            }

            //Save whenever the panel loses focus (i.e. the user clicks on another tab or closes it)
            panel.KeyboardFocusChanged += async (o, _) =>
            {
                Panel p = (Panel) o!;

                if (!p.IsKeyboardFocused)
                {
                    await WriteKeybindsToFile(nameKeybindDict, keybindsPath).ConfigureAwait(false);
                }
            };
        }

        private static async Task WriteKeybindsToFile(Dictionary<string, string> nameKeybindDict, string outputPath)
        {
            await using StreamWriter writer = new(outputPath);

            foreach (var (name, keybind) in nameKeybindDict)
            {
                if (name is "#HELD" or "#NOTHELD")
                {
                    await writer.WriteLineAsync(name).ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteLineAsync($"{name}: {keybind}").ConfigureAwait(false);
                }
            }
        }
    }
}