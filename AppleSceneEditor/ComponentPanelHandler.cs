using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Wrappers;
using AppleSerialization.Json;
using Myra.Graphics2D.UI;
using AssetManagementBase.Utility;
using GrappleFightNET5.Scenes;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using JsonProperty = AppleSerialization.Json.JsonProperty;


//TODO: Overall: Reduce line count and verbosity. Feel like I can do a bit better here in terms of this.
namespace AppleSceneEditor
{
    /// <summary>
    /// Responsible for manipulating <see cref="StackPanel"/> instances so that they can be used to mainpulate the
    /// properties of entity components.
    /// </summary>
    public partial class ComponentPanelHandler
    {
        private const int MaxTextLength = 25;
        private const int DefaultTextBoxFontSize = 18;
        private const int IndentationIncrement = 8;

#nullable disable
        private Window _selectElemTypeWindow;
        private Window _selectArrElemTypeWindow;
#nullable enable

        private JsonObject _rootObject;
        
        public Desktop Desktop { get; set; }

        public JsonObject RootObject
        {
            get => _rootObject;
            set
            {
                _rootObject = value;
                
                JsonArray? components = _rootObject.FindArray("components");
                Components = components ?? throw new ComponentsNotFoundException(_rootObject.Name);

                RebuildUI();
            }
        }
        
        public JsonArray Components { get; private set; }
        
        public StackPanel PropertyStackPanel { get; set; }
        
        public List<IComponentWrapper> Wrappers { get; set; }

        public ComponentPanelHandler(Desktop desktop, JsonObject rootObject, StackPanel propertyStackPanel)
        {
            Wrappers = new List<IComponentWrapper>();
            
            (Desktop, PropertyStackPanel, RootObject) = (desktop, propertyStackPanel, rootObject);
            
            (_selectElemTypeWindow, _selectArrElemTypeWindow) = (new Window(), new Window());
        }
        
        //------------------
        // Public methods
        //------------------
        
        public bool SaveToScene(Scene scene)
        {
            if (scene.ScenePath is null)
            {
                Debug.WriteLine($"{nameof(SaveToScene)}: scene does not have a ScenePath! Cannot save to scene.");
                return false;
            }

            if (_rootObject.Name is null)
            {
                Debug.WriteLine($"{nameof(SaveToScene)}: the rootObject does not have a name! Cannot save to scene.");
                return false;
            }

            _rootObject.GenerateEntity(scene, scene.ScenePath is null
                ? null
                : Path.Combine(scene.ScenePath, "Entities", $"{_rootObject.Name}.entity"));

            return true;
        }
        
        /// <summary>
        /// Reconstructs and rebuilds any UI elements associated with this object.
        /// </summary>
        /// <param name="rootObject">The <see cref="JsonObject"/> to display. If not set to, then an internal root
        /// object will be used instead.</param>
        public void RebuildUI(JsonObject? rootObject = null)
        {
            rootObject ??= _rootObject;

            PropertyStackPanel.Widgets.Clear();
            //re-add the holder labels so that the first actual elements are not hidden by the options bar.
            PropertyStackPanel.AddChild(new Label {Text = "Holder"});
            
            BuildUI();
        }
        
        //--------------------------
        // Private methods & more
        //--------------------------
        
        private void BuildUI()
        {
            foreach (JsonObject jsonObj in Components)
            {
                IComponentWrapper? wrapper = ComponentWrapperExtensions.CreateFromObject(jsonObj, Desktop);
                if (wrapper is null) continue;

                PropertyStackPanel.AddChild(wrapper.UIPanel);
            }
        }
        
        public sealed class ComponentsNotFoundException : Exception
        {
            public ComponentsNotFoundException(string? entityId) : base(
                $"Cannot find component array in Entity with an id of {entityId ?? "(Entity has no id)"}!")
            {
            }
        }

        //------------------
        // Enums and misc.
        //------------------

        private enum JsonElementType
        {
            Property,
            Array,
            Object,
        }

        private enum JsonPropertyType
        {
            Boolean,
            Integer,
            Float,
            String,
        }
    }
}