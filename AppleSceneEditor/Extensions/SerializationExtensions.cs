using System;
using System.Diagnostics;
using System.Linq;
using AppleSerialization;
using AppleSerialization.Json;
using GrappleFightNET5.Components.Transform;
using Microsoft.Xna.Framework;

namespace AppleSceneEditor.Extensions
{
    //TODO: Add docs
    public static class SerializationExtensions
    {
        public static void UpdateTransform(this JsonObject obj, Transform transform)
        {
#if DEBUG
            const string methodName = nameof(SerializationExtensions) + "." + nameof(UpdateTransform);
#endif
            JsonArray? components = obj.FindArray("components");

            if (components is null)
            {
                Debug.WriteLine($"{methodName}: cannot find components array.");
                return;
            }
            
            //find the TransformInfo object
            JsonObject? transformObject = (from component in components.Objects
                where component.FindProperty("$type").Value as string == "TransformInfo"
                select component).FirstOrDefault();
            
            if (transformObject is null)
            {
                Debug.WriteLine($"{methodName}: cannot find TransformInfo object!");
                return;
            }

            //The transform object SHOULD have all of the required properties (it was checked before hand)
            JsonProperty? positionProp = transformObject.FindProperty("position");
            JsonProperty? scaleProp = transformObject.FindProperty("scale");
            JsonProperty? rotationProp = transformObject.FindProperty("rotation");
            JsonProperty? velocityProp = transformObject.FindProperty("velocity");

            if (positionProp is null || scaleProp is null || rotationProp is null || velocityProp is null)
            {
                throw new Exception("Transform property is missing.");
            }

            transform.Matrix.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);

            /*
             * TODO: Getting euler angles from quaternions may lead to some ugly results.
             * We might be able to get away with it now, but keep an eye on it.
             */
            //It's important to note that converting a quaternion to euler angles may result in different values than
            //originally (although they still represent the same exact angle as the original)
            positionProp.Value = ToSpacedStr(translation);
            scaleProp.Value = ToSpacedStr(scale);
            rotationProp.Value = ToSpacedStr(GetEulerAnglesFromQuaternion(rotation));
            velocityProp.Value = ToSpacedStr(transform.Velocity);
        }

        private static Vector3 GetEulerAnglesFromQuaternion(Quaternion q)
        {
            float poleValue = q.X * q.Y + q.Z * q.W;
            bool isPole = Math.Abs(MathF.Abs(poleValue) - 0.5f) < 0.00001f;

            float heading = isPole
                ? MathF.Sign(poleValue) * 2 * MathF.Atan2(q.X, q.W)
                : MathF.Atan2((2f * q.Y * q.W) - (2f * q.X * q.Z), 1f - 2f * (q.Y * q.Y) - 2f * (q.Z * q.Z));

            float attitude = MathF.Asin((2f * q.X * q.Y) + (2f * q.Z * q.W));

            float bank = MathF.Atan2(isPole
                ? 0
                : (2f * q.X * q.W) - (2f * q.Y * q.Z), 1f - 2f * (q.X * q.X) - 2f * (q.Z * q.Z));

            return new Vector3(heading, attitude, bank);
        }

        private static string ToSpacedStr(Vector3 value) => $"{value.X} {value.Y} {value.Z}";
    }
}