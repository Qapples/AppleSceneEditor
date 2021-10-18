using System;
using System.Collections.Generic;
using AppleSceneEditor.Extensions;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;

namespace AppleSceneEditor.Wrappers
{
    /*
     * TODO: It's already kinda too late for this but the way we handle ComponentWrappers is kinda sucky.
     * Tons of boilerplate. Reliance on tests (not an inherently bad thing but it's still quite iffy). Overall bad.
     * Not terrible, still usable, but it can be a lot better. Not entirly sure  what to do since I'm going to have to
     * do quite a bit of refactoring in order to fix this. But, if I continue on like this, it might be too late to fix
     * it where it can cause much more issues in the future. For now I just want to focus on making a game though, it'll
     * have to be for now.
     */
    
    /// <summary>
    /// Represents an object that wraps around a <see cref="JsonObject"/> of a specified type and exposes data that
    /// the user can manipulate through the property editor. WARNING: Any implementers must also implement a public
    /// static field "AssociatedType" that dictates the type the wrapper is supported to wrap around.
    /// <remarks>In order to create instances, use
    /// <see cref="AppleSceneEditor.Extensions.ComponentWrapperExtensions.CreateFromObject(AppleSerialization.Json.JsonObject, Myra.Graphics2D.UI.Desktop)"/>
    /// or <br/>
    /// <see cref="ComponentWrapperExtensions.CreateFromObject(AppleSerialization.Json.JsonObject, Myra.Graphics2D.UI.Desktop, System.Type?)"/>.
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
        /// All classes that implement <see cref="IComponentWrapper"/>. The key a type and the value is the wrapper
        /// associated with that type (if there is one)
        /// </summary>
        public static Dictionary<Type, Type> Implementers => ComponentWrapperExtensions.Implementers;
    }
}