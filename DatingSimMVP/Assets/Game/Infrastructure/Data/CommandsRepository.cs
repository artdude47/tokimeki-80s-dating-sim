using System.Collections.Generic;
using System.IO;
using Game.Domain.Commands;
using Newtonsoft.Json;
using UnityEngine;


namespace Game.Infrustructure.Data
{
    public interface ICommandsRepository
    {
        Dictionary<string, CommandDef> Load(string absolutePath);
    }

    public sealed class CommandsRepository : ICommandsRepository
    {
        public Dictionary<string, CommandDef> Load(string absolutePath)
        {
            if (!File.Exists(absolutePath))
            {
                Debug.LogError($"commands.json not found: {absolutePath}");
                return new Dictionary<string, CommandDef>();
            }

            var txt = File.ReadAllText(absolutePath);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, CommandDef>>(txt);
            foreach (var kv in dict) kv.Value.id = kv.Key;
            return dict;
        }
    }
}
