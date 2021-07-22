using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppleSceneEditor.Serialization.Converters;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AppleSceneEditor.Serialization
{
    /// <summary>
    /// Static class that provides methods that assist in working with serialization and deserialization
    /// </summary>
    public static class ConverterHelper
    {
        /// <summary>
        /// Associates types (Vector2, etc.) with their corresponding converter class
        /// </summary>
        private static readonly Dictionary<Type, Type> TypeConverterMap = new()
        {
            {typeof(Color), typeof(ColorJsonConverter)},
            {typeof(FontSystem), typeof(FontSystemJsonConverter)},
            {typeof(Texture2D), typeof(Texture2DJsonConverter)},
            {typeof(Vector2), typeof(Vector2JsonConverter)}
        };

        /// <summary>
        /// All types that are not converted in the TypeConverterMap are listed here
        /// </summary>
        // we're not using the in type reference to match the name of the methods of the Utf8JsonReader struct
        // .GetBoolean(), .GetByte(), .GetSingle(), etc.
        private static readonly Dictionary<Type, GetDelegate> ExcludedTypes = new()
        {
            {typeof(Boolean), (ref Utf8JsonReader reader) => reader.GetBoolean()},
            {typeof(Byte), (ref Utf8JsonReader reader) => reader.GetByte()},
            {typeof(DateTime), (ref Utf8JsonReader reader) => reader.GetDateTime()},
            {typeof(Decimal), (ref Utf8JsonReader reader) => reader.GetDecimal()},
            {typeof(Double), (ref Utf8JsonReader reader) => reader.GetDouble()},
            {typeof(Guid), (ref Utf8JsonReader reader) => reader.GetGuid()},
            {typeof(Int16), (ref Utf8JsonReader reader) => reader.GetInt16()},
            {typeof(Int32), (ref Utf8JsonReader reader) => reader.GetInt32()},
            {typeof(Int64), (ref Utf8JsonReader reader) => reader.GetInt64()},
            {typeof(SByte), (ref Utf8JsonReader reader) => reader.GetSByte()},
            {typeof(Single), (ref Utf8JsonReader reader) => reader.GetSingle()},
            {typeof(String), (ref Utf8JsonReader reader) => reader.GetString()},
            {typeof(UInt16), (ref Utf8JsonReader reader) => reader.GetUInt16()},
            {typeof(UInt32), (ref Utf8JsonReader reader) => reader.GetUInt32()},
            {typeof(UInt64), (ref Utf8JsonReader reader) => reader.GetUInt64()},
        };

        /// <summary>
        /// Delegate used in ExcludedTypes
        /// </summary>
        private delegate object? GetDelegate(ref Utf8JsonReader reader);

        /// <summary>
        /// Returns an instance of data defined by a single value (ex: a string, an integer, a vector, a color, etc).
        /// </summary>
        /// <param name="reader">Utf8JsonReader used to provide the data for getting an object</param>
        /// <param name="type">The type of data. Decides what converter to use</param>
        /// <param name="options">Options associated with the Utf8JsonReader</param>
        /// <returns>If there is a converter associated with the type parameter, then an object that has been passed
        /// through the associated converter is returned. If there is no such converter, then null is returned.
        /// If, for any reason, the conversion fails, null is also returned and a debug message is displayed
        /// </returns>
        public static object? GetValueFromReader(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            //if the type already has an existing read method in Utf8JsonReader, then just use that method.
            if (ExcludedTypes.TryGetValue(type, out var getDelegate))
            {
                return getDelegate(ref reader);
            }

            //we're doing alot of null checks here just to be safe! we're working in a nullable reference types
            //environment
            if (!TypeConverterMap.TryGetValue(type, out Type converterType))
            {
                Debug.WriteLine(
                    $"Cannot get converter for the type {type}! GetValueFromReader is returning null");
                return null;
            }

            object? converterInstance = Activator.CreateInstance(converterType);
            if (converterInstance is null)
            {
                Debug.WriteLine($"Activator.CreateInstance returned null of type {converterType}. " +
                                $"GetValueFromReader is returning null");
                return null;
            }

            var readerHelperType = typeof(ReadHelper<>).MakeGenericType(type);
            if (Activator.CreateInstance(readerHelperType, converterInstance) is not ReadHelper readHelper)
            {
                Debug.WriteLine($"Unable to create ReadHelper<{type}>. GetValueFromReader is returning null");
                return null;
            }

            return readHelper.Read(ref reader, type, options);
        }

        /// <summary>
        /// Returns an array of objects that represents the serialization of each element in a Json array.
        /// </summary>
        /// <param name="reader">Utf8JsonReader instance that provides the data to deserialize. The token type of the
        /// reader MUST initially be "TokenType.StartArray" or else the function will fail.</param>
        /// <param name="options">Options associated with the Utf8JsonReader</param>
        /// <returns>If successful, returns an array of objects that represents the serialization of each element in a
        /// Json array. If unsuccessful, null is returned and a message is written to the debugger</returns>
        public static object?[]? GetArrayFromReader(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                Debug.WriteLine($"reader token type is initially not StartArray in GetArrayFromReader! Returning" +
                                $" null. (Reader token type: {reader.TokenType})");
                return null;
            }

            //valueType is going to be immutabletext or some other
            List<object?> outputList = new();

            while (reader.TokenType != JsonTokenType.EndArray)
            {
                Type? valueType = GetTypeFromObject(ref reader);
                if (valueType is null)
                {
                    Debug.WriteLine($"Type is null (GetArrayFromReader). Skipping...");
                    continue;
                }

                Type deserializeHelperType =
                    typeof(DeserializeHelper<>).MakeGenericType(Nullable.GetUnderlyingType(valueType!) ?? valueType!);

                if (Activator.CreateInstance(deserializeHelperType) is DeserializeHelper
                    deserializeHelperInstance)
                {
                    outputList.Add(deserializeHelperInstance.Deserialize(ref reader, options));
                }

                reader.Read();
            }

            return outputList.ToArray();
        }

        /// <summary>
        /// Finds the $type property in a Json object and returns the corresponding type. Returns null if not found.
        /// </summary>
        private static Type? GetTypeFromObject(ref Utf8JsonReader reader)
        {
            while ((reader.TokenType != JsonTokenType.PropertyName || reader.GetString()! != "$type") &&
                   reader.TokenType != JsonTokenType.EndObject)
            {
                reader.Read();
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                Debug.WriteLine($"$type specifier was not found in the object and the type could not be " +
                                $"determined!. GetTypeFromObject (private) returning null.");
                return null;
            }

            reader.Read();

            //TODO: chance project name might change. account for that later 
            string? typeStr = reader.GetString();
            Type? valueType = Type.GetType($"{typeStr!}, GrappleFightNET5");

            if (typeStr is null)
            {
                Debug.WriteLine($"Can't find type of name {typeStr!}! GetTypeFromObject (private) returning null.");
                return null;
            }

            return valueType;
        }

        /// <summary>
        /// Returns data that is defined by using object syntax (ex: Borders)
        /// </summary>
        /// <param name="reader">Utf8JsonReader instance that provides the data to deserialize. It is preferable that
        /// the TokenType property first be "JsonTokenType.StartObject"</param>
        /// <param name="type">The type of the object to deserialize</param>
        /// <param name="options">Options associated with the Utf8JsonReader</param>
        /// <returns>Returns an object that is representative of the Json data provided by the reader. If null, then
        /// the deserialization was unsuccessful</returns>
        public static object? GetObjectFromReader(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            Type deserializeHelperType =
                typeof(DeserializeHelper<>).MakeGenericType(Nullable.GetUnderlyingType(type) ?? type);

            if (Activator.CreateInstance(deserializeHelperType) is DeserializeHelper deserializeHelperInstance)
            {
                return deserializeHelperInstance.Deserialize(ref reader, options);
            }

            return null;
        }


        //we can't pass values by reference using reflection, so we use this hacky solution to do so
        //(https://stackoverflow.com/questions/60830084/how-to-pass-an-argument-by-reference-using-reflection)

        /// <summary>
        /// Abstract class that allows the calling of Deserialize with a reference to a UTf8JsonReader as a parameter
        /// within a reflection context with a delegate hack. Based off of
        /// (https://stackoverflow.com/questions/60830084/how-to-pass-an-argument-by-reference-using-reflection)
        /// </summary>
        public abstract class ReadHelper
        {
            public abstract object? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);
        }

        private class ReadHelper<T> : ReadHelper
        {
            private delegate T? ReadDelegate(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);

            private readonly ReadDelegate? _readDelegate;

            public ReadHelper(JsonConverter<T> converter)
            {
                _readDelegate = converter.Read;
            }

            public override object? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
            {
                if (_readDelegate is null) return null;

                return _readDelegate.Invoke(ref reader, type, options);
            }
        }

        /// <summary>
        /// Abstract class that allows the calling of Deserialize with a reference to a UTf8JsonReader as a parameter
        /// within a reflection context with a delegate hack. Based off of
        /// https://stackoverflow.com/questions/60830084/how-to-pass-an-argument-by-reference-using-reflection
        /// </summary>
        public abstract class DeserializeHelper
        {
            public abstract object? Deserialize(ref Utf8JsonReader reader, JsonSerializerOptions options);
        }

        /// <summary>
        /// Allows the DeserializeHelper abstract class to call Deserialize with a specified generic/return type
        /// </summary>
        /// <typeparam name="T">The return type of the Deserialize method</typeparam>
        public class DeserializeHelper<T> : DeserializeHelper
        {
            private delegate T? DeserializeDelegate(ref Utf8JsonReader reader, JsonSerializerOptions options);

            private readonly DeserializeDelegate? _deserializeDelegate;

            public DeserializeHelper()
            {
                _deserializeDelegate = Serializer<T>.Deserialize;
            }

            public override object? Deserialize(ref Utf8JsonReader reader,
                JsonSerializerOptions options)
            {
                if (_deserializeDelegate is null) return null;

                return _deserializeDelegate.Invoke(ref reader, options);
            }
        }
    }
}