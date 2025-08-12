using Game.Domain.Time;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace Game.Infrustructure.Data
{
    public interface ICalendarRepository
    {
        SchoolCalendarDef Load(string absolutePath);
    }

    public sealed class CalendarRepository : ICalendarRepository
    {
        public SchoolCalendarDef Load(string absolutePath)
        {
            if (!File.Exists(absolutePath))
            {
                Debug.LogError($"Calendar not found: {absolutePath}");
                return new SchoolCalendarDef
                {
                    years = new()
                };
            }
            return JsonConvert.DeserializeObject<SchoolCalendarDef>(File.ReadAllText(absolutePath));
        }
    }
}
