using System;
using AppleSerialization;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Wrappers
{
    public interface IComponentWrapper
    {
        JsonObject JsonObject { get; set; }
        
        Panel UIPanel { get; set; }
        
        bool IsEmpty { get; }
        
        Type AssociatedType { get; }
    }
}