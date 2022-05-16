using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using GrappleFightNET5.Collision;
using GrappleFightNET5.Collision.Components;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
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

            InternalChild.AddChild(_opcodesScrollViewer);
            InternalChild.AddChild(_hullScrollViewer);

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
    }
}