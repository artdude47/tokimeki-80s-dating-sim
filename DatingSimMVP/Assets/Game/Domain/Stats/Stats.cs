namespace Game.Domain.Stats 
{
    public enum Stat { Academics, Art, Athletics, Stamina, Charm, Guts, Stress, GenKnowledge }

    public sealed class StatBlock
    {
        const int MIN = 0, MAX = 999;
        private readonly System.Collections.Generic.Dictionary<Stat, int> _values = new System.Collections.Generic.Dictionary<Stat, int>();

        public StatBlock()
        {
            foreach (Stat s in System.Enum.GetValues(typeof(Stat))) _values[s] = 0;
        }

        public int this[Stat s] => _values[s];

        public void Add(Stat s, int delta)
        {
            var v = _values[s] + delta;
            if (v < MIN) v = MIN;
            if (v > MAX) v = MAX;
            _values[s] = v;
        }
    }
}
