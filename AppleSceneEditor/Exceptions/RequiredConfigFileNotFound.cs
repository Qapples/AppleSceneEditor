using System;

namespace AppleSceneEditor.Exceptions
{
    internal class RequiredConfigFileNotFound : Exception
    {
        private const string MessagePrefix = "The required config files were not found (by name)";

        public RequiredConfigFileNotFound(string message) : base(message)
        {
        }

        public RequiredConfigFileNotFound(params string[] missingFileNames) : base(
            $"{MessagePrefix}: {string.Join(", ", missingFileNames)}")
        {
        }
    }
}