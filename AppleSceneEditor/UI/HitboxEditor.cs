using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Input;
using AppleSceneEditor.Input.Commands;
using AppleSerialization;
using DefaultEcs;
using GrappleFightNET5.Collision;
using GrappleFightNET5.Collision.Components;
using GrappleFightNET5.Collision.Hulls;
using GrappleFightNET5.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using Myra.Graphics2D.UI.Styles;

namespace AppleSceneEditor.UI
{
    public class HitboxEditor : SingleItemContainer<Grid>, IDisposable
    {
        public string _hitboxFilePath;

        public string HitboxFilePath
        {
            get => _hitboxFilePath;
            set
            {
                _hitboxFilePath = value;
                LoadHitboxFile(value);
            }
        }

        private TextBox _opcodesTextBox;
        private TextBox _hullsTextBox;
        
        private ScrollViewer _opcodesScrollViewer;
        private ScrollViewer _hullScrollViewer;

        private FileDialog _openFileDialog;
        private FileDialog _saveFileDialog;
        
        private HorizontalMenu _menuBar;
        
        private World _world;
        
        private InputHandler _heldInputHandler;
        
        private GraphicsDevice _graphicsDevice;
        private BasicEffect _hitboxEffect;
        private VertexBuffer _vertexBuffer;
        
        private HitboxData? _hitboxData;
        private Camera _hitboxDrawCamera; //camera used for drawing the hitboxes
        
        /// <summary>
        /// Section of space on the entire screen where the hulls are actually drawn.
        /// </summary>
        private Viewport _hitboxDrawSection;

        private const float OpcodesTextBoxProportion = 3f / 4f;
        private const float HullTextBoxProportion = 1f - OpcodesTextBoxProportion;

        private const int MenuBarHeight = 20;
        
        public HitboxEditor(TreeStyle? style, GraphicsDevice graphicsDevice, string keybindFilePath)
        {
            _world = new World();

            _graphicsDevice = graphicsDevice;
            _hitboxEffect = new BasicEffect(graphicsDevice)
            {
                Alpha = 1,
                VertexColorEnabled = true,
                LightingEnabled = false
            };
            _vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), 36, BufferUsage.WriteOnly);

            _hitboxDrawSection = new Viewport();
            
            _world.Set(new Camera(Vector3.Zero,
                Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(95f),
                    graphicsDevice.DisplayMode.AspectRatio, 1f, 1000f), _hitboxDrawSection, 2f)
            {
                LookAt = new Vector3(0f, 0f, 20f)
            });
            
            _world.Set(new CameraProperties
            {
                YawDegrees = 0f,
                PitchDegrees = 0f,
                CameraSpeed = 0.5f
            });

            _heldInputHandler = new InputHandler(keybindFilePath, TryGetCommandFromFunctionName, true);

            if (style is not null)
            {
                ApplyWidgetStyle(style);
            }

            InternalChild = new Grid
            {
                ColumnSpacing = 4,
                RowSpacing = 1,
                AcceptsKeyboardFocus = true
            };

            InternalChild.ColumnsProportions.Add(new Proportion(ProportionType.Part, 2.5f));
            //InternalChild.RowsProportions.Add(new Proportion(ProportionType.Part, 3f / 1f));
            InternalChild.RowsProportions.Add(new Proportion(ProportionType.Auto));
            
            const int borderThickness = 1;
            
            _opcodesTextBox = new TextBox
            {
                Multiline = true, 
                TextVerticalAlignment = VerticalAlignment.Stretch, 
                Border = new SolidBrush(Color.White),
                BorderThickness = new Thickness(borderThickness, borderThickness, 0, borderThickness),
                AcceptsKeyboardFocus = true
            };
            
            _hullsTextBox = new TextBox
            {
                Multiline = true,
                TextVerticalAlignment = VerticalAlignment.Stretch, 
                Border = new SolidBrush(Color.White),
                BorderThickness = new Thickness(borderThickness, 0, 0, 0),
                AcceptsKeyboardFocus = true
            };
            
            _opcodesScrollViewer = new ScrollViewer
            {
                GridColumn = 1, 
                GridRow = 1,
                Content = _opcodesTextBox
            };
            
            _hullScrollViewer = new ScrollViewer
            {
                GridColumn = 1, 
                GridRow = 0,
                Content = _hullsTextBox
            };

            _openFileDialog = new FileDialog(FileDialogMode.OpenFile)
            {
                Filter = "*.gfhb",
                Enabled = true,
                Visible = true
            };
            
            _saveFileDialog = new FileDialog(FileDialogMode.SaveFile)
            {
                Filter = "*.gfhb",
                Enabled = true,
                Visible = true
            };

            _menuBar = new HorizontalMenu
            {
                Items =
                {
                    new MenuItem("OpenMenuItem", "Open"),
                    new MenuItem("SaveMenuItem", "Save")
                }
            };

            _hitboxDrawSection = new Viewport(Bounds.X, Bounds.Y - MenuBarHeight, InternalChild.GetColumnWidth(0),
                Bounds.Height - MenuBarHeight);

            _menuBar.FindMenuItemById("OpenMenuItem").Selected += (_, _) => _openFileDialog.ShowModal(Desktop);
            _menuBar.FindMenuItemById("SaveMenuItem").Selected += (_, _) => _saveFileDialog.ShowModal(Desktop);

            _openFileDialog.Closed += OpenFileDialogClosed;
            _saveFileDialog.Closed += SaveFileDialogClosed;

            InternalChild.AddChild(_opcodesScrollViewer);
            InternalChild.AddChild(_hullScrollViewer);
            InternalChild.AddChild(_menuBar);

            if (Parent?.Height is not null)
            {
                int height = Parent.Height.Value - InternalChild.ColumnSpacing;

                _opcodesTextBox.MinHeight = (int) (height * OpcodesTextBoxProportion);
                _hullsTextBox.MinHeight = (int) (height * HullTextBoxProportion);
            }

            SizeChanged += (_, _) =>
            {
                if (Height is not null)
                {
                    int height = Height.Value - InternalChild.ColumnSpacing;

                    _opcodesTextBox.MinHeight = (int) (height * OpcodesTextBoxProportion);
                    _hullsTextBox.MinHeight = (int) (height * HullTextBoxProportion) - 5;
                }
            };
        }

        public HitboxEditor(GraphicsDevice graphicsDevice, string keybindFilePath) : this(Stylesheet.Current.TreeStyle,
            graphicsDevice, keybindFilePath)
        {
        }

        public HitboxEditor(GraphicsDevice graphicsDevice, string hitboxFilePath, string keybindFilePath) :
            this(Stylesheet.Current.TreeStyle, graphicsDevice, hitboxFilePath, keybindFilePath)
        {
            HitboxFilePath = hitboxFilePath;
        }

        public HitboxEditor(TreeStyle? style, GraphicsDevice graphicsDevice, string hitboxFilePath,
            string keybindFilePath) : this(style, graphicsDevice, keybindFilePath)
        {
            HitboxFilePath = hitboxFilePath;
        }

        public void LoadHitboxFile(string hitboxFilePath)
        {
            using FileStream fs = File.Open(hitboxFilePath, FileMode.OpenOrCreate);
            using BinaryReader reader = new(fs, Encoding.UTF8, false);

            StringBuilder opcodesTextBuilder = new();
            StringBuilder hullsTextBuilder = new();

            //---------- LOAD HULLS ---------- 
            byte hullCount = reader.ReadByte();

            for (int hullId = 0; hullId < hullCount; hullId++)
            {
                CollisionHullTypes hullType = (CollisionHullTypes) reader.ReadByte();

                hullsTextBuilder.Append($"type: {hullType}\n");

                switch (hullType)
                {
                    case CollisionHullTypes.ComplexBox:
                        hullsTextBuilder.Append($"{ToSpaceStr(ReadVector3(reader))}\n"); //CenterOffset
                        hullsTextBuilder.Append($"{ToSpaceStr(ReadVector4(reader))}\n"); //RotationOffset
                        hullsTextBuilder.Append($"{ToSpaceStr(ReadVector3(reader))}\n");   //HalfExtent

                        break;
                }

                hullsTextBuilder.Append('\n');
            }

            //---------- LOAD OPCODES ----------

            ushort opcodeCount = reader.ReadUInt16();

            for (int i = 0; i < opcodeCount; i++)
            {
                float time = reader.ReadSingle();
                HitboxOpcodes opcode = (HitboxOpcodes) reader.ReadByte();
                ushort parametersLength = reader.ReadUInt16();

                opcodesTextBuilder.Append($"{time} {opcode}\n");

                byte hitboxId = reader.ReadByte();
                opcodesTextBuilder.Append(hitboxId);
                opcodesTextBuilder.Append('\n');

                switch (opcode)
                {
                    //Act and Deact only have one parameter, which is just the hitboxId.

                    case HitboxOpcodes.Alt:
                    case HitboxOpcodes.Slt:
                        Vector3 translation = ReadVector3(reader);
                        opcodesTextBuilder.Append($"{translation.X} {translation.Y} {translation.Z}\n");

                        break;

                    case HitboxOpcodes.Alrac:
                    case HitboxOpcodes.Slrac:
                        Vector4 rotation = ReadVector4(reader);
                        opcodesTextBuilder.Append($"{rotation.X} {rotation.Y} {rotation.Z} {rotation.W}\n");

                        break;
                }

                opcodesTextBuilder.Append('\n');
            }

            _hullsTextBox.Text = hullsTextBuilder.ToString();
            _opcodesTextBox.Text = opcodesTextBuilder.ToString();
        }

        public void SaveHitboxFileContents(string fileLocation, string hullContents, string opcodeContents)
        {
            using (FileStream fs = File.Open(fileLocation, FileMode.OpenOrCreate))
            {
                using BinaryWriter writer = new(fs, Encoding.UTF8, false);
                
                //------------- SAVE HULLS ------------- 

                MatchCollection typeRegexMatches = new Regex("type: (.+)").Matches(hullContents);

                byte hullCount = (byte) typeRegexMatches.Count;
                writer.Write(hullCount);

                string[] hullContentLines = hullContents.Split('\n');
                int lineIndex = 0;

                for (int hullId = 0; hullId < hullCount; hullId++)
                {
                    string hullType = typeRegexMatches[hullId].Groups[1].Value;
                    int bufferIndex = 0;
                    lineIndex++; //skip the line that defines the type of the hull.

                    switch (Enum.Parse<CollisionHullTypes>(hullType))
                    {
                        case CollisionHullTypes.ComplexBox:
                            ParseHelper.TryParseVector3(hullContentLines[lineIndex++], out Vector3 centerOffset);
                            ParseHelper.TryParseVector4(hullContentLines[lineIndex++], out Vector4 rotationVector4);
                            ParseHelper.TryParseVector3(hullContentLines[lineIndex++], out Vector3 halfExtent);

                            writer.Write((byte) CollisionHullTypes.ComplexBox);

                            WriteVector3(writer, centerOffset);
                            WriteVector4(writer, rotationVector4);
                            WriteVector3(writer, halfExtent);

                            //skip whitespace line that separate the hulls.
                            while (lineIndex < hullContentLines.Length &&
                                   string.IsNullOrWhiteSpace(hullContentLines[lineIndex]))
                            {
                                lineIndex++;
                            }

                            break;
                        case CollisionHullTypes.LineSegment:
                            break;
                    }
                }

                // ------------- SAVE OPCODES -------------

                string[] opcodeContentLines = opcodeContents.Split('\n');
                ushort opcodeCount = 0;

                //skip two bytes for room for a ushort that will represent the number of opcodes
                int opcodeCountPos = (int) writer.BaseStream.Position; //position of this ushort in the byte stream
                writer.Seek(2, SeekOrigin.Current);

                for (int lineI = 0; lineI < opcodeContentLines.Length; lineI++)
                {
                    if (string.IsNullOrWhiteSpace(opcodeContentLines[lineI]))
                    {
                        continue;
                    }

                    string[] opcodeAndTimeSplitArr = opcodeContentLines[lineI++].Split(' ');

                    int bufferIndex = 0;

                    float time = float.Parse(opcodeAndTimeSplitArr[0]);
                    HitboxOpcodes opcode = Enum.Parse<HitboxOpcodes>(opcodeAndTimeSplitArr[1]);

                    writer.Write(time);
                    writer.Write((byte) opcode);

                    switch (opcode)
                    {
                        case HitboxOpcodes.Act:
                        case HitboxOpcodes.Deact:
                            ushort parameterLength = 1;

                            writer.Write(parameterLength);
                            writer.Write(byte.Parse(opcodeContentLines[lineI++]));

                            break;

                        case HitboxOpcodes.Alt:
                        case HitboxOpcodes.Slt:
                            parameterLength = 13;
                            byte hullId = byte.Parse(opcodeContentLines[lineI++]);
                            ParseHelper.TryParseVector3(opcodeContentLines[lineI++], out Vector3 translation);

                            writer.Write(parameterLength);
                            writer.Write(hullId);
                            WriteVector3(writer, translation);

                            break;

                        case HitboxOpcodes.Alrac:
                        case HitboxOpcodes.Slrac:
                            parameterLength = 17;
                            hullId = byte.Parse(opcodeContentLines[lineI++]);
                            ParseHelper.TryParseVector4(opcodeContentLines[lineI++], out Vector4 rotationVector4);

                            writer.Write(parameterLength);
                            writer.Write(hullId);
                            WriteVector4(writer, rotationVector4);

                            break;
                    }

                    opcodeCount++;
                }

                writer.Seek(opcodeCountPos, SeekOrigin.Begin);
                writer.Write(opcodeCount);
                writer.Seek(0, SeekOrigin.End);
                writer.Flush();
            }
            
            _hitboxData = new HitboxData(File.ReadAllBytes(fileLocation), _world.CreateEntity());
        }

        public void UpdateCamera(ref KeyboardState kbState, ref KeyboardState previousKbState)
        {
            if (!Visible || !InternalChild.IsKeyboardFocused || _hullsTextBox.IsKeyboardFocused ||
                _opcodesTextBox.IsKeyboardFocused)
            {
                return;
            }

            IKeyCommand[] heldCommands = _heldInputHandler.GetCommands(ref kbState, ref previousKbState);

            foreach (IKeyCommand command in heldCommands)
            {
                if (command is EmptyCommand) break;
                
                command.Execute();
            }
        }

        public void Draw()
        {
            if (!_hitboxData.HasValue || !Visible) return;

            Viewport previousViewport = _graphicsDevice.Viewport;
            
            _hitboxDrawSection = new Viewport(Bounds.X, Bounds.Y + MenuBarHeight, InternalChild.GetColumnWidth(0),
                Bounds.Height - MenuBarHeight);
            _graphicsDevice.Viewport = _hitboxDrawSection;

            HitboxData hitboxData = _hitboxData.Value;
            Matrix identity = Matrix.Identity;
            
            hitboxData.Draw(_graphicsDevice, _hitboxEffect, Color.Blue, ref identity, ref _hitboxDrawCamera,
                WireframeState, _vertexBuffer);

            _graphicsDevice.Viewport = previousViewport;
        }

        public void Dispose()
        {
            _opcodesTextBox = null!;
            _hullsTextBox = null!;

            _opcodesScrollViewer = null!;
            _hullScrollViewer = null!;

            _openFileDialog = null!;
            _saveFileDialog = null!;

            _menuBar = null!;

            _world.Dispose();
            _world = null!;
            
            _heldInputHandler.Dispose();
            _heldInputHandler = null!;

            _graphicsDevice = null!;
            
            _hitboxEffect.Dispose();
            _hitboxEffect = null!;
            
            _vertexBuffer.Dispose();
            _vertexBuffer = null!;
        }

        private static Vector3 ReadVector3(BinaryReader reader) =>
            new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        private static Vector4 ReadVector4(BinaryReader reader) =>
            new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        private static void WriteVector3(BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        private static void WriteVector4(BinaryWriter writer, Vector4 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        private static string ToSpaceStr(Vector3 value) => $"{value.X} {value.Y} {value.Z}";
        private static string ToSpaceStr(Vector4 value) => $"{value.X} {value.Y} {value.Z} {value.W}";

        private void OpenFileDialogClosed(object? sender, EventArgs? args)
        {
            if (!_openFileDialog.Result || string.IsNullOrEmpty(_openFileDialog.FilePath))
            {
                return;
            }

            LoadHitboxFile(_openFileDialog.FilePath);
        }

        private void SaveFileDialogClosed(object? sender, EventArgs? args)
        {
            if (!_saveFileDialog.Result || string.IsNullOrEmpty(_saveFileDialog.FilePath))
            {
                return;
            }

            SaveHitboxFileContents(_saveFileDialog.FilePath, _hullsTextBox.Text, _opcodesTextBox.Text);
        }

        private bool TryGetCommandFromFunctionName(string funcName, out IKeyCommand command) =>
            (command = funcName switch
            {
                "move_camera_forward" =>
                    new MoveCameraCommand(MovementHelper.Direction.Forward, _world),
                "move_camera_backward" =>
                    new MoveCameraCommand(MovementHelper.Direction.Backwards, _world),
                "move_camera_left" =>
                    new MoveCameraCommand(MovementHelper.Direction.Left, _world),
                "move_camera_right" =>
                    new MoveCameraCommand(MovementHelper.Direction.Right, _world),
                "move_camera_up" =>
                    new MoveCameraCommand(MovementHelper.Direction.Up, _world),
                "move_camera_down" =>
                    new MoveCameraCommand(MovementHelper.Direction.Down, _world),
                _ => IKeyCommand.EmptyCommand
            }) is not EmptyCommand;

        private static readonly RasterizerState WireframeState = new()
            {FillMode = FillMode.WireFrame, CullMode = CullMode.None};
    }
}