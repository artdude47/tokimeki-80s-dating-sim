using System.Collections.Generic;
using Game.Domain.Common;
using Game.Domain.Stats;
using Game.Domain.Time;
using Game.Domain.Relationships;
using System.ComponentModel;
using Codice.Client.BaseCommands.Admin;

namespace Game.Infrastructure.Dialogue
{
    public interface IDialogueVariablesProvider
    {
        Dictionary<string, object> Snapshot();
    }

    public sealed class DialogueVariablesProvider : IDialogueVariablesProvider
    {
        private readonly ITimeService _time;
        private readonly StatBlock _stats;
        private readonly RelationshipState _rels;
        private readonly BombService _bombs;

        public DialogueVariablesProvider(ITimeService time, StatBlock stats, RelationshipState rels, BombService bombs)
        {
            _time = time;
            _stats = stats;
            _rels = rels;
            _bombs = bombs;
        }

        public Dictionary<string, object> Snapshot()
        {
            var d = new Dictionary<string, object>
            {
                ["YEAR"] = _time.Current.Year,
                ["WEEK"] = _time.Current.Week,
                ["DOW"] = _time.Current.Day.ToString(),
                ["PHASE"] = _time.CurrentPhase.ToString(),

                ["ACAD"] = _stats[Stat.Academics],
                ["ART"] = _stats[Stat.Art],
                ["ATH"] = _stats[Stat.Athletics],
                ["STA"] = _stats[Stat.Stamina],
                ["CHARM"] = _stats[Stat.Charm],
                ["GUTS"] = _stats[Stat.Guts],
                ["STRESS"] = _stats[Stat.Stress],
                ["GK"] = _stats[Stat.GenKnowledge],
            };

            // Known NPCs hardcoded for MVP TODO: drive from data
            AddNpc(d, "npc_ash");
            AddNpc(d, "npc_jen");
            AddNpc(d, "npc_max");
            return d;
        }

        private void AddNpc(Dictionary<string, object> d, string npcId)
        {
            d[$"AFF_{npcId}"] = _rels.GetAffection(npcId);
            d[$"BOMB_{npcId}"] = _bombs.IsArmed(npcId);
        }
    }
}
