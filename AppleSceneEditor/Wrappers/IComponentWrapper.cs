using System;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Wrappers
{
    /// <summary>
    /// Represents an object that wraps around a <see cref="JsonObject"/> of a specified type and exposes data that
    /// the user can manipulate through the property editor.
    /// <remarks>In order to create instances, use
    /// <see cref="AppleSceneEditor.Extensions.ComponentWrapperExtensions.CreateFromType(AppleSerialization.Json.JsonObject, string)"/>
    /// or <br/>
    /// <see cref="AppleSceneEditor.Extensions.ComponentWrapperExtensions.CreateFromType(AppleSerialization.Json.JsonObject, Type)"/>.
    /// <br/> Do not attempt to use constructors.
    /// </remarks>
    /// </summary>
    public interface IComponentWrapper
    {
        /// <summary>
        /// Represents the component to wrap around.
        /// </summary>
        JsonObject JsonObject { get; set; }

        /// <summary>
        /// <see cref="Panel"/> containing <see cref="Widget"/> instances that is displayed to the user in the property
        /// editor and additional occasions.
        /// </summary>
        Panel UIPanel { get; set; }
        
        /// <summary>
        /// If true, then there is no data associated with this wrapper or creation of the the wrapper failed. Otherwise,
        /// false.
        /// </summary>
        bool IsEmpty { get; }
        
        /// <summary>
        /// The type of data the wrapper is targeting.
        /// </summary>
        Type AssociatedType { get; }
    }
}