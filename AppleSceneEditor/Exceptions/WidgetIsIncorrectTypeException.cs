using System;
using System.Collections.Generic;

namespace AppleSceneEditor.Exceptions
{
    public class WidgetIsIncorrectTypeException : Exception
    {
        public WidgetIsIncorrectTypeException(Type requiredType, params string[] invalidObjectNames) : base(
            $"Widgets are not of the correct type! Desired type: {requiredType}.\n" +
            (invalidObjectNames.Length > 0
                ? $"Name of objects that are NOT of this type are: {string.Join(", ", invalidObjectNames)}"
                : ""))
        {
        }

        public WidgetIsIncorrectTypeException(Type requiredType, params (string name, object? obj)[] objAndNames) : this(
            requiredType, GetInvalidNamesFromTuple(requiredType, objAndNames))
        {
        }

        private static string[] GetInvalidNamesFromTuple(Type requiredType, (string name, object? obj)[] objAndNames)
        {
            List<string> invalidNames = new();

            foreach (var (name, obj) in objAndNames)
            {
                if (obj?.GetType() != requiredType)
                {
                    invalidNames.Add(name);
                }
            }

            return invalidNames.ToArray();
        }
    }
}