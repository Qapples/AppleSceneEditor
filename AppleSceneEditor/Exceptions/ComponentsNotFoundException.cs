using System;

namespace AppleSceneEditor.Exceptions
{
    internal sealed class ComponentsNotFoundException : Exception
    {
        public ComponentsNotFoundException(string? entityId) : base(
            $"Cannot find component array in Entity with an id of {entityId ?? "(Entity has no id)"}!")
        {
        }
    }
}