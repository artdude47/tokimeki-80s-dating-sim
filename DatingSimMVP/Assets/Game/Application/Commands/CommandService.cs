using System;
using System.Collections.Generic;
using System.Xml.Schema;
using Game.Domain.Commands;
using Game.Domain.Common;
using Game.Domain.CommonEvents;
using Game.Domain.Stats;
using Game.Domain.Time;

namespace Game.Application.Commands
{
    public interface IRandomizer
    {
        int Range(int minInclusive, int maxInclusive);
    }

    public sealed class SysRandomizer : IRandomizer
    {
        private readonly Random _r = new Random();
        public int Range(int min, int max) => _r.Next(min, max);
    }

    public interface ICommandService
    {
        void SetCatalog(Dictionary<string, CommandDef> catalog);
        void SelectWeekdayCommand(string id);
        void OnPhaseStarted(PhaseStarted e);
        string CurrentCommandId { get; }
    }

    public sealed class CommandService : ICommandService
    {
        private readonly EventBus _bus;
        private readonly StatBlock _stats;
        private readonly IRandomizer _rng;
        private Dictionary<string, CommandDef> _catalog;
        private string _currentCommand;
        private string _lastWeekCommand;
        private int _sameCommandStreak = 0;

        public string CurrentCommandId => _currentCommand;
        public string LastWeekCommandId => _lastWeekCommand;
        public int SameCommandStreak => _sameCommandStreak;

        public CommandService(EventBus bus, StatBlock stats, IRandomizer rng = null)
        {
            _bus = bus;
            _stats = stats;
            _rng = rng ?? new SysRandomizer();
            _bus.Subscribe<PhaseStarted>(OnPhaseStarted);
            _bus.Subscribe<WeekStarted>(e => OnWeekStarted());
        }

        public void SetCatalog(Dictionary<string, CommandDef> catalog) => _catalog = catalog;

        public void SelectWeekdayCommand(string id)
        {
            if (_catalog == null || !_catalog.ContainsKey(id))
                throw new ArgumentException("$Unknown command id '{id}'");
            _currentCommand = id;
        }

        private void OnWeekStarted()
        {
            if (!string.IsNullOrEmpty(_lastWeekCommand) && _currentCommand == _lastWeekCommand)
                _sameCommandStreak++;
            else
                _sameCommandStreak = 0;

            _lastWeekCommand = _currentCommand;
        }

        public void RestoreState(string currentCommandId, string lastWeekCommandId, int sameCommandStreak)
        {
            _currentCommand = currentCommandId;
            _lastWeekCommand = lastWeekCommandId;
            _sameCommandStreak = sameCommandStreak < 0 ? 0 : sameCommandStreak;
        }

        public void OnPhaseStarted(PhaseStarted e)
        {
            if (e.Phase != Phase.Weekday) return;
            if (string.IsNullOrEmpty(_currentCommand) || _catalog == null) return;

            var def = _catalog[_currentCommand];
            var deltas = new Dictionary<Stat, int>();

            void add(Stat s, int v)
            {
                if (!deltas.ContainsKey(s)) deltas[s] = 0;
                deltas[s] += v;
                _stats.Add(s, v);
            }

            //compute decay factor for repeated weeks
            float decay = 1f;
            if (_sameCommandStreak > 0 && def.repeatDecay > 0f && def.repeatDecay < 1.0001f)
                decay = (float)Math.Pow(def.repeatDecay, _sameCommandStreak);

            // INC Ranges
            if (def.inc != null)
            {
                foreach (var sr in def.inc)
                {
                    var s = CommandDefExtensions.ParseStat(sr.stat);
                    int raw = _rng.Range(sr.min, sr.max);
                    int val = (int)Math.Floor(raw * decay);
                    if (val != 0) add(s, val);
                }
            }

            // DEC ranges
            if (def.dec != null)
            {
                foreach (var sr in def.dec)
                {
                    var s = CommandDefExtensions.ParseStat(sr.stat);
                    int raw = _rng.Range(sr.min, sr.max);
                    int val = (int)Math.Floor(raw * decay);
                    if (val != 0) add(s, -val);
                }
            }

            //Stress & Stamina
            if (def.stress != null)
            {
                int v = _rng.Range(def.stress.min, def.stress.max);
                if (v != 0) add(Stat.Stress, v);
            }
            if (def.staminaCost != null)
            {
                int v = _rng.Range(def.staminaCost.min, def.staminaCost.max);
                if (v != 0) add(Stat.Stamina, -v);
            }

            _bus.Publish(new StatsChanged(deltas));
        }
    }
}
