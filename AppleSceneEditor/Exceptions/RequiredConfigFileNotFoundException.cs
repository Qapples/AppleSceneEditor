using System;

namespace AppleSceneEditor.Exceptions
{
    //this name is really long. Can't really think of a better name :(.
    internal class RequiredConfigFileNotFoundException : Exception
    {
        private const string MessagePrefix = "The required config files were not found (by name)";

        public RequiredConfigFileNotFoundException(string message) : base(message)
        {
        }

        public RequiredConfigFileNotFoundException(params string[] missingFileNames) : base(
            $"{MessagePrefix}: {string.Join(", ", missingFileNames)}")
        {
        }
    }
}