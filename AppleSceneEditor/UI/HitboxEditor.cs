using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using AppleSceneEditor.Extensions;
using AppleSerialization;
using GrappleFightNET5.Collision;
using GrappleFightNET5.Collision.Components;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using Myra.Graphics2D.UI.Styles;

namespace AppleSceneEditor.UI
{
    public class HitboxEditor : SingleItemContainer<Grid>
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

        private const float OpcodesTextBoxProportion = 3f / 4f;
        private const float HullTextBoxProportion = 1f - OpcodesTextBoxProportion;
        
        public HitboxEditor(TreeStyle? style)
        {
            if (style is not null)
            {
                ApplyWidgetStyle(style);
            }

            InternalChild = new Grid
            {
                ColumnSpacing = 4,
                RowSpacing = 1,
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
            };
            
            _hullsTextBox = new TextBox
            {
                Multiline = true,
                TextVerticalAlignment = VerticalAlignment.Stretch, 
                Border = new SolidBrush(Color.White),
                BorderThickness = new Thickness(borderThickness, 0, 0, 0),
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
        
        public HitboxEditor() : this(Stylesheet.Current.TreeStyle)
        {
        }

        public HitboxEditor(string hitboxFilePath) : this(Stylesheet.Current.TreeStyle)
        {
            HitboxFilePath = hitboxFilePath;
        }

        public HitboxEditor(TreeStyle? style, string hitboxFilePath) : this(style)
        {
            HitboxFilePath = hitboxFilePath;
        }

        public void LoadHitboxFile(string hitboxFilePath)
        {
            Span<byte> hitboxFileBytes = File.ReadAllBytes(hitboxFilePath).AsSpan();
            int byteIndex = 0;

            StringBuilder opcodesTextBuilder = new();
            StringBuilder hullsTextBuilder = new();

            //---------- LOAD HULLS ---------- 
            byte hullCount = hitboxFileBytes[byteIndex++];

            for (int hullId = 0; hullId < hullCount; hullId++)
            {
                CollisionHullTypes hullType = (CollisionHullTypes) (hitboxFileBytes[byteIndex++]);

                hullsTextBuilder.Append($"type: {hullType}");

                switch (hullType)
                {
                    // 00 = ComplexBox
                    //  12 bytes that represent the CenterOffset
                    //  16 bytes that represent the RotationOffset
                    //  12 bytes that represent the HalfExtent
                    case CollisionHullTypes.ComplexBox:
                        Vector3 centerOffset =
                            MemoryMarshal.Read<Vector3>(hitboxFileBytes[byteIndex..(byteIndex += 12)]);
                        Quaternion rotationOffset =
                            MemoryMarshal.Read<Quaternion>(hitboxFileBytes[byteIndex..(byteIndex += 16)]);
                        Vector3 halfExtent =
                            MemoryMarshal.Read<Vector3>(hitboxFileBytes[byteIndex..(byteIndex += 12)]);

                        hullsTextBuilder.Append($"CenterOffset: {centerOffset}");
                        hullsTextBuilder.Append($"RotationOffset: {rotationOffset}");
                        hullsTextBuilder.Append($"HalfExtent: {halfExtent}");

                        break;
                }
            }

            //---------- LOAD OPCODES ----------

            byte opcodeCount = hitboxFileBytes[byteIndex++];

            for (int i = 0; i < opcodeCount; i++)
            {
                float time = MemoryMarshal.Read<float>(hitboxFileBytes[byteIndex..(byteIndex += 4)]);
                HitboxOpcodes opcode = (HitboxOpcodes) (hitboxFileBytes[byteIndex++]);
                ushort parametersLength = MemoryMarshal.Read<ushort>(hitboxFileBytes[byteIndex..(byteIndex += 2)]);

                opcodesTextBuilder.Append($"{time} {opcode}\n");

                byte hitboxId = hitboxFileBytes[byteIndex++];
                opcodesTextBuilder.Append(hitboxId);

                switch (opcode)
                {
                    //Act and Deact only have one parameter, which is just the hitboxId.

                    case HitboxOpcodes.Alt:
                    case HitboxOpcodes.Slt:
                        Vector3 translation =
                            MemoryMarshal.Read<Vector3>(hitboxFileBytes[byteIndex..(byteIndex += 12)]);
                        opcodesTextBuilder.Append($"{translation.X} {translation.Y} {translation.Z}");

                        break;

                    case HitboxOpcodes.Alrac:
                    case HitboxOpcodes.Slrac:
                        Quaternion rotation =
                            MemoryMarshal.Read<Quaternion>(hitboxFileBytes[byteIndex..(byteIndex += 16)]);
                        opcodesTextBuilder.Append($"{rotation.X} {rotation.Y} {rotation.Z} {rotation.W}");

                        break;
                }

                opcodesTextBuilder.Append('\n');
            }

            _hullsTextBox.Text = hullsTextBuilder.ToString();
            _opcodesTextBox.Text = _opcodesTextBox.ToString();
        }

        public void SaveHitboxFilContents(string fileLocation, string hullContents, string opcodeContents)
        {
            using FileStream fs = File.Open(fileLocation, FileMode.OpenOrCreate);
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
                        while (string.IsNullOrWhiteSpace(hullContentLines[lineIndex++])) ;

                        break;
                    case CollisionHullTypes.LineSegment:
                        break;
                }
            }
            
            // ------------- SAVE OPCODES -------------
            
            string[] opcodeContentLines = opcodeContents.Split('\n');

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
                        writer.Write(opcodeContentLines[lineI++]);

                        break;

                    case HitboxOpcodes.Alt:
                    case HitboxOpcodes.Slt:
                        parameterLength = 12;
                        ParseHelper.TryParseVector3(opcodeContentLines[lineI++], out Vector3 translation);
                        
                        writer.Write(parameterLength);
                        WriteVector3(writer, translation);

                        break;
                    
                    case HitboxOpcodes.Alrac:
                    case HitboxOpcodes.Slrac:
                        parameterLength = 16;
                        ParseHelper.TryParseVector4(opcodeContentLines[lineI++], out Vector4 rotationVector4);
                        
                        writer.Write(parameterLength);
                        WriteVector4(writer, rotationVector4);

                        break;
                }
            }
        }

        private Vector3 ReadVector3(BinaryReader reader) =>
            new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        private Vector4 ReadVector4(BinaryReader reader) =>
            new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        private void WriteVector3(BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        private void WriteVector4(BinaryWriter writer, Vector4 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        private void OpenFileDialogClosed(object? sender, EventArgs? args)
        {
            if (!_openFileDialog.Result || string.IsNullOrEmpty(_saveFileDialog.FilePath))
            {
                return;
            }

            LoadHitboxFile(_saveFileDialog.FilePath);
        }

        private void SaveFileDialogClosed(object? sender, EventArgs? args)
        {
            if (!_saveFileDialog.Result || string.IsNullOrEmpty(_saveFileDialog.FilePath))
            {
                return;
            }

            using FileStream fs = File.Create(_saveFileDialog.FilePath);
            
            LoadHitboxFile(_saveFileDialog.FilePath);
        }
    }
}