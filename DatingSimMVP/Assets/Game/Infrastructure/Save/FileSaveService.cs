using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Game.Domain.Save;

namespace Game.Infrustructure.Save
{
    public interface ISaveService
    {
        void Save(GameState state, string fileName = "save.json");
        GameState Load(string fileName = "save.json");
        string DefaultPath(string fileName = "save.json");
    }

    public sealed class FileSaveService : ISaveService
    {
        public string DefaultPath(string fileName = "save.json")
            => Path.Combine(UnityEngine.Application.persistentDataPath, fileName);

        public void Save(GameState state, string fileName = "save.json")
        {
            var path = DefaultPath(fileName);
            File.WriteAllText(path, JsonConvert.SerializeObject(state, Formatting.Indented));
        }

        public GameState Load(string fileName = "save.json")
        {
            var path = DefaultPath(fileName);
            if (!File.Exists(path)) return null;
            return JsonConvert.DeserializeObject<GameState>(File.ReadAllText(path));
        }
    }
}
