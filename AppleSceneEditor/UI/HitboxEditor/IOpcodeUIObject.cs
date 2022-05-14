using System;
using Myra;

namespace AppleSceneEditor.UI.HitboxEditor
{
    public interface IOpcodeUIObject
    {
        void ToBytes(in Span<byte> bytesDestination);
    }
}