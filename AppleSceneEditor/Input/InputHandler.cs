using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using AppleSceneEditor.Extensions;
using AppleSceneEditor.Input.Commands;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Input
{
    public class InputHandler : IDisposable
    {
        private Dictionary<string, CommandEntry> _commands;
        private IKeyCommand[] _outgoingCommands;

        private static readonly IKeyCommand[] EmptyCommands = Array.Empty<IKeyCommand>();

        public delegate bool CommandFromFunctionNameDelegate(string funcName, out IKeyCommand command);
        
        public bool Disposed { get; private set; }
        public bool CanBeHeld { get; init; }

        /// <summary>
        /// Determines how many commands can be returned at once from GetCommand methods.
        /// </summary>
        public const int MaxCommandsActivated = 5;
#if DEBUG
        private const string MaxCmdName = nameof(MaxCommandsActivated);
#endif
        
        public InputHandler(Dictionary<string, CommandEntry> commands, bool canBeHeld)
        {
            _commands = commands;
            
            //we have fill the array the old fashiuon 
            _outgoingCommands = new IKeyCommand[MaxCommandsActivated];
            for (int i = 0; i < _outgoingCommands.Length; i++)
            {
                _outgoingCommands[i] = new EmptyCommand();
            }
            
            CanBeHeld = canBeHeld;
            Disposed = false;
        }
        
        public InputHandler(bool canBeHeld) : this(new Dictionary<string, CommandEntry>(), canBeHeld)
        {
        }

        
        public InputHandler(string filePath, CommandFromFunctionNameDelegate tryGetCommandFromFunctionName,
            bool canBeHeld) : this(new Dictionary<string, CommandEntry>(), canBeHeld)
        {
#if DEBUG
            const string methodName = nameof(InputHandler) +
                                      " (string filePath, CommandFromFunctionNameDelegate tryGetCommandFromFunctionName, bool canBeHeld) constructor):";
#endif
            using StreamReader reader = new(filePath, Encoding.ASCII);
            
            string? line = reader.ReadLine();
            while (line is not null)
            {
                //# indicates a region. start looking for data after a region.
                if (!string.IsNullOrEmpty(line) && line[0] == '#' && line[1..] == (canBeHeld ? "HELD" : "NOTHELD"))
                {
                    _commands = GetFunctionMapFromStream(reader, tryGetCommandFromFunctionName, out line);
                }
                else
                {
                    line = reader.ReadLine();
                }
            }

            if (_commands.Count == 0)
            {
                Debug.WriteLine($"{methodName}: unable to find commands.");
            }
        }

        public IKeyCommand[] GetCommands(ref KeyboardState kbState, ref KeyboardState prevKbState)
        {
#if DEBUG
            const string methodName = nameof(InputHandler) + "." + nameof(GetCommands);
#endif
            if (!CanBeHeld && kbState == prevKbState)
            {
                return EmptyCommands;
            }

            int index = 0;
            
            foreach (CommandEntry command in _commands.Values)
            {
                if (AllKeysPressed(ref kbState, command.Keys))
                {
                    if (index == MaxCommandsActivated)
                    {
                        Debug.WriteLine($"{methodName}: index has reached the value of {MaxCmdName} " +
                                        $"({MaxCommandsActivated})! Stopping prematurely.");
                        return _outgoingCommands;
                    }

                    _outgoingCommands[index++] = command.Command;
                }
            }
            
            //if the index is zero then that indicates that no commands have been activated
            if (index == 0) return EmptyCommands;
            
            //clean up the last commands as they might have been set beforehand
            for (; index < MaxCommandsActivated; index++)
            {
                _outgoingCommands[index] = IKeyCommand.EmptyCommand;
            }

            return _outgoingCommands;
        }

        public void UpdateCommandKeys(string commandName, params Keys[] keys)
        {
#if DEBUG
            const string methodName = nameof(InputHandler) + "." + nameof(UpdateCommandKeys);
#endif
            if (!_commands.TryGetValue(commandName, out var oldCmd))
            {
                Debug.WriteLine($"{methodName}: cannot find and update command of name {commandName}!");
                return;
            }

            _commands[commandName] = new CommandEntry(keys, oldCmd.Command);
        }

        public void Dispose()
        {
            foreach (CommandEntry command in _commands.Values)
            {
                command.Command.Dispose();
            }

            foreach (IKeyCommand command in _outgoingCommands)
            {
                if (!command.Disposed) command.Dispose();
            }

            (_commands, _outgoingCommands, Disposed) = (null!, null!, true);
        }

        public Dictionary<string, CommandEntry> GetFunctionMapFromStream(StreamReader reader,
            CommandFromFunctionNameDelegate tryGetCommandFromFunctionName, out string? lastLine)
        {
            const string methodName = nameof(InputHandler) + "." + nameof(GetFunctionMapFromStream);

            Dictionary<string, CommandEntry> commands = new();
            
            //'#' indicates a region. stop searching when we hit a new region.
            string? line;
            while ((line = reader.ReadLine()) is not null && line[0] != '#')
            {
                int colonIndex = line.IndexOf(':');

                if (colonIndex > 0)
                {
                    //there should only be ONE space between the colon and the key. account for the space and colon by
                    //adding two
                    string funcName = line[..colonIndex];
                    string keysStr = line[(colonIndex + 2)..];

                    if (tryGetCommandFromFunctionName(funcName, out var command))
                    {
                        Keys[] keys = KeyboardExtensions.ParseKeyboardState(keysStr);
                        
                        if (keys.Length > 0)
                        {
                            commands[funcName] = new CommandEntry(keys, command);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"{methodName}: cannot get function name from following line: {line}");
                    }
                }
                else
                {
                    Debug.WriteLine($"{methodName}: cannot find func name behind colon in the following line: " +
                                    $"{line}. Skipping.");
                }
            }

            lastLine = line;
            return commands;
        }

        //we have this method here as using linq would cause a bunch of closure captures and since GetCommand will be
        //called frequently it could potentially cause a memory leak.
        private bool AllKeysPressed(ref KeyboardState kbState, Keys[] keys)
        {
            foreach (Keys key in keys)
            {
                if (kbState.IsKeyUp(key)) return false;
            }

            return true;
        }
    }
}