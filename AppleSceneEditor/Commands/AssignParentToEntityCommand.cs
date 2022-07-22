using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.UI;
using AppleSerialization.Json;
using DefaultEcs;
using GrappleFightNET5.Components;
using GrappleFightNET5.Resource.Info;
using Myra.Graphics2D.UI;
using JsonProperty = AppleSerialization.Json.JsonProperty;

namespace AppleSceneEditor.Commands
{
    public class AssignParentToEntityCommand : IEditorCommand
    {
        public bool Disposed { get; }
        
        public string ParentId { get; set; }
        public string ChildId { get; set; }
        public JsonObject ChildJsonObject { get; set; }

        private EntityViewer _entityViewer;

        private JsonObject _parentJsonObject;
        private JsonProperty _parentNameProp;

        private VerticalStackPanel? _parentStackPanel;
        private VerticalStackPanel? _childStackPanel;
        private Grid? _childGrid;

        private string? _previousParentName;

        //long variable name. too bad!
        private bool _childJsonObjectHasParentComponent;

        public AssignParentToEntityCommand(string parentId, string childId, JsonObject childJsonObject,
            EntityViewer entityViewer)
        {
            ParentId = parentId;
            ChildId = childId;
            ChildJsonObject = childJsonObject;
            _entityViewer = entityViewer;

            JsonArray? childComponentsJson = ChildJsonObject.FindArray("components");
            if (childComponentsJson is null)
            {
                return;
            }

            JsonObject? parentJsonObject = (from component in childComponentsJson.Objects
                where component.FindProperty("$type").Value as string == nameof(ParentInfo)
                select component).FirstOrDefault();
            _childJsonObjectHasParentComponent = parentJsonObject is not null;

            _parentJsonObject = parentJsonObject ?? new JsonObject(null, ChildJsonObject);
            _parentNameProp = _parentJsonObject.FindProperty("parentId") ??
                              new JsonProperty("parentId", parentId, _parentJsonObject, JsonValueKind.String);
            _previousParentName = _parentJsonObject.FindProperty("parentId")?.Value as string;

            _parentStackPanel = _entityViewer.EntityButtonStackPanel
                .TryFindWidgetById<Grid>($"{EntityViewer.EntityGridIdPrefix}{ParentId}")
                ?.TryFindWidgetById<VerticalStackPanel>(EntityViewer.WidgetStackPanelName);
            _childGrid =
                _entityViewer.EntityButtonStackPanel.TryFindWidgetById<Grid>(
                    $"{EntityViewer.EntityGridIdPrefix}{ChildId}");
            _childStackPanel = _childGrid?.Parent.Parent as VerticalStackPanel; //i have no clue why is it two parents.

            if (!_childJsonObjectHasParentComponent)
            {
                _parentJsonObject.Properties.Add(new JsonProperty("$type", nameof(ParentInfo), _parentJsonObject,
                    JsonValueKind.String));
                _parentJsonObject.Properties.Add(_parentNameProp);
            }
        }


        public void Execute()
        {
            if (_childGrid is not null)
            {
                _childGrid.RemoveFromParent();
                _parentStackPanel?.AddChild(_childGrid);
            }

            if (_childJsonObjectHasParentComponent)
            {
                _parentNameProp.Value = ParentId;
            }
            else
            {
                JsonArray? childComponentsJson = ChildJsonObject.FindArray("components");
                childComponentsJson?.Add(_parentJsonObject);
                _parentJsonObject.Parent = ChildJsonObject;
            }
        }

        public void Undo()
        {
            JsonArray? childComponentsJson = ChildJsonObject.FindArray("components");
            
            if (_childJsonObjectHasParentComponent && !string.IsNullOrEmpty(_previousParentName))
            {
                _parentNameProp.Value = _previousParentName; 
            }
            else if (childComponentsJson is not null)
            {
                childComponentsJson.Remove(_parentJsonObject);
                _parentJsonObject.Parent = null;
            }

            if (_childGrid is not null)
            {
                _childGrid.RemoveFromParent();
                _childStackPanel?.AddChild(_childGrid);
            }
        }

        public void Redo() => Execute();

        public void Dispose()
        {
        }
    }
}