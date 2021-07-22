using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DefaultEcs.Serialization;
using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;
using Scene = SharpGLTF.Schema2.Scene;

namespace AppleSceneEditor.Serialization
{
    /// <summary>
    /// Class responsible for loading and reloading assets that are NOT handled via the content pipeline.
    /// </summary>
    public sealed class RawContentManager : IDisposable
    {
        /// <summary>
        /// The base directory will assets be loaded in from.
        /// </summary>
        public string Directory { get; set; }

        private readonly Dictionary<string, object> _loadedAssets = new (StringComparer.OrdinalIgnoreCase);
        private readonly List<IDisposable?> _disposables = new();
        private GraphicsDevice _graphicsDevice;

        /// <summary>
        /// Constructs a RawContentManager from just a GraphicsDevice instance. The Directory property will be defaulted
        /// to <see cref="GlobalVars.ContentPath"/>.
        /// </summary>
        /// <param name="graphicsDevice">GraphicsDevice instance used to load in specific graphic related assets like
        /// textures, etc. </param>
        public RawContentManager(GraphicsDevice graphicsDevice) : this(graphicsDevice, GlobalVars.ContentPath)
        {
        }

        /// <summary>
        /// Constructs a RawContentManager from a GraphicsDevice instance and a directory representing the location
        /// where assets will be loaded from
        /// </summary>
        /// <param name="graphicsDevice">GraphicsDevice instance used to load in specific graphic related assets like
        /// textures, etc. </param>
        /// <param name="directory">The directory will assets will be loaded in from.</param>
        public RawContentManager(GraphicsDevice graphicsDevice, string directory)
        {
            (_graphicsDevice, Directory) = (graphicsDevice, directory);
        }
        
        /// <summary>
        /// Loads in an asset from a specified location (from the Directory property) of type T.
        /// </summary>
        /// <param name="assetLocation">The location and name of the asset relative to the Directory property</param>
        /// <param name="args">Additional arguments. Currently unused.</param>
        /// <typeparam name="T">The type of content being loaded.</typeparam>
        /// <returns>If successful, an instance of type T loaded in from the specified directory is returned.
        /// If unsuccessful, then null is returned and a message is written to the debugger</returns>
        public T? Load<T>(string assetLocation, object?[]? args = null) where T : class
        {
            if (_loadedAssets.TryGetValue(assetLocation, out var value)) return value as T;
            
            //assetPath is where the content is relative to the Directory property
            string assetPath = Path.Combine(Directory, assetLocation);

            //TODO: Potentially repeating code. Add a method that accepts a delegate that is the method used to get the resource?
            switch (typeof(T))
            {
                case
                    var type when type == typeof(Texture2D):
                {
                    try
                    {
                        var texture = Texture2D.FromStream(_graphicsDevice, File.OpenRead(assetPath));

                        _loadedAssets.Add(assetPath, texture);
                        _disposables.Add(texture);

                        return texture as T;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Unable to find texture of path {assetPath}. Returning null. Exception: {e}");
                        return null;
                    }
                }
                case
                    var type when type == typeof(Effect):
                {
                    try
                    {
                        var effect = new Effect(_graphicsDevice, File.ReadAllBytes(assetPath));

                        _loadedAssets.Add(assetPath, effect);
                        _disposables.Add(effect);

                        return effect as T;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Unable to load shader of path {assetPath}. Returning null. Exception: {e}");
                        return null;
                    }
                }
                case
                    var type when type == typeof(Scene):
                {
                    try
                    {
                        using Stream worldStream = File.OpenRead(assetPath);

                        var world = new TextSerializer().Deserialize(worldStream);
                        
                        _loadedAssets.Add(assetPath, world);
                        _disposables.Add(world);

                        return world as T;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Unable to load world of path {assetPath}. Returning null. Exception: {e}");
                        return null;
                    }
                }
                default:
                    Debug.WriteLine(
                        $"{typeof(T)} is not supported by the Load method and cannot be loaded. Returning null");
                    return null;
            }
        }

        /// <summary>
        /// Loads multiple assets at once.
        /// </summary>
        /// <param name="assetLocations">Locations of the assets to load</param>
        /// <param name="args">Additional arguments. Currently unused.</param>
        /// <typeparam name="T">The type of content being loaded.</typeparam>
        /// <returns>An array representing all assets that have loaded successfully is returned. If no asset is loaded
        /// successfully, null is returned.</returns>
        public T[]? LoadArr<T>(string[] assetLocations, object?[]? args = null) where T : class
        {
            List<T> output = new();
            
            foreach (string asset in assetLocations)
            {
                object? loadedAsset = Load<T>(asset, args);

                if (loadedAsset is not null) output.Add((loadedAsset as T)!);
            }

            return output.Count < 1 ? null : output.ToArray();
        }

        /// <summary>
        /// Loads multiple assets and assigns them under one instance/name. Useful for asset types that use factories
        /// or require multiple other assets to be complete. One example being FontSystems.
        /// </summary>
        /// <param name="assetLocations">The locations (relative to the Directory property) to load in. It is
        /// recommended to use Directory.GetDirectories(string) to get subdirectories.</param>
        /// <param name="instance">The instance of type T that handles the assets.</param>
        /// <param name="name">The name of the instance.</param>
        /// <param name="args">Additional arguments. Currently unused.</param>
        /// <typeparam name="T">Type of the overarching instance</typeparam>
        /// <returns>If successful, an instance of type T is returned. If unsuccessful, null is returned</returns>
        public T? LoadFactory<T>(string[] assetLocations, T? instance, string name,
            object?[]? args = null) where T : class
        {
            if (_loadedAssets.TryGetValue(name, out var value)) return value as T;
            
            //assetPath is where the content is relative to the Directory property
            string[] assetPaths = assetLocations.Select(e => Path.Combine(Directory, e)).ToArray();

            switch (typeof(T))
            {
                case
                    var type when type == typeof(FontSystem):
                {
                    //Instance can be null regardless if it is marked as nullable or not. It is marked as nullable
                    //as to let the compiler know that it can be null
                    if (instance is null)
                    {
                        Debug.WriteLine("The instance parameter cannot be null in LoadFactory. Returning null.");
                        return null;
                    }

                    FontSystem fontSystem = (instance as FontSystem)!;
                    foreach (string path in assetPaths)
                    {
                        try
                        {
                           fontSystem.AddFont(File.ReadAllBytes(Path.Combine(Directory, path))); 
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine($"Error loading font from {path}. Ignoring file. Exception: {e}");
                        }
                    }
                    
                    _loadedAssets.Add(name, fontSystem);
                    _disposables.Add(fontSystem);

                    return fontSystem as T;
                }
                default:
                    Debug.WriteLine(
                        $"{typeof(T)} is not supported by the Load method and cannot be loaded. Returning null");
                    return null;
            }
        }

        /// <summary>
        /// Asynchronous version of the <see cref="Load{T}"/> method. <br/>
        /// Loads in an asset from a specified location (from the Directory property) of type T.
        /// </summary>
        /// <param name="assetLocation">The location and name of the asset relative to the Directory property</param>
        /// <param name="args">Additional arguments. Currently unused.</param>
        /// <typeparam name="T">The type of content being loaded.</typeparam>
        /// <returns>If successful, an instance of type T loaded in from the specified directory is returned.
        /// If unsuccessful, then null is returned and a message is written to the debugger</returns>
        public async Task<T?> LoadAsync<T>(string assetLocation, object?[]? args = null) where T : class =>
            await Task.Run(() => Load<T>(assetLocation, args));

        /// <summary>
        /// Asynchronous version of the <see cref="LoadArr{T}"/>method. <br/>
        /// Loads multiple assets at once.
        /// </summary>
        /// <param name="assetLocations">Locations of the assets to load</param>
        /// <param name="args">Additional arguments. Currently unused.</param>
        /// <typeparam name="T">The type of content being loaded.</typeparam>
        /// <returns>An array representing all assets that have loaded successfully is returned. If no asset is loaded
        /// successfully, null is returned.</returns>
        public async Task<T[]?> LoadArrAsync<T>(string[] assetLocations, object?[]? args = null) where T : class =>
            await Task.Run(() => LoadArr<T>(assetLocations, args));

        /// <summary>
        /// Asynchronous version of the <see cref="LoadFactory{T}"/> method. <br/>
        /// Loads multiple assets and assigns them under one instance/name. Useful for asset types that use factories
        /// or require multiple other assets to be complete. One example being FontSystems.
        /// </summary>
        /// <param name="assetLocations">The locations (relative to the Directory property) to load in.</param>
        /// <param name="instance">The instance of type T that handles the assets.</param>
        /// <param name="name">The name of the instance.</param>
        /// <param name="args">Additional arguments. Currently unused.</param>
        /// <typeparam name="T">Type of the overarching instance</typeparam>
        /// <returns>If successful, an instance of type T is returned. If unsuccessful, null is returned</returns>
        public async Task<T?> LoadFactoryAsync<T>(string[] assetLocations, T? instance, string name,
            object?[]? args = null) where T : class =>
            await Task.Run(() => LoadFactory(assetLocations, instance, name, args));

        /// <summary>
        /// Removes a loaded (and if possible disposes) a loaded object/asset
        /// </summary>
        /// <param name="assetName">Name of the asset to remove and dispose (if possible)</param>
        /// <returns>True if the asset was already loaded and disposed/removed. False if the asset was not found and
        /// has not yet been loaded.</returns>
        public bool Unload(string assetName)
        {
            if (_loadedAssets[assetName] is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            return _loadedAssets.Remove(assetName);
        }
        
        /// <summary>
        /// Disposes all disposable objects that have been loaded and disposes this object as well
        /// </summary>
        public void Dispose()
        {
            foreach (var disposable in _disposables) disposable?.Dispose();
        }
    }
}