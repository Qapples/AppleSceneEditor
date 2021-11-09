using System;

namespace AppleSceneEditor.Exceptions
{
    internal class WidgetNotFoundException : Exception
    {
        private const string MessagePrefix = "The following widgets could not be found (by name)";

        public WidgetNotFoundException(string message) : base(message)
        {
        }

        public WidgetNotFoundException(params string[] widgetNames) : base(
            $"{MessagePrefix}: {string.Join(", ", widgetNames)}")
        {
        }
    }
}