namespace Game.Domain.Relationships
{
    public sealed class Npc
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int InitialAffection { get; }

        public Npc(string id, string name, int initialAffection = 0)
        {
            Id = id;
            DisplayName = name;
            InitialAffection = initialAffection;
        }
    }

    public static class Npcs
    {
        public static readonly Npc Ash = new("npc_ash", "Ashley", 0);
        public static readonly Npc Jen = new("npc_jen", "Jen", 0);
        public static readonly Npc Max = new("npc_max", "Max", 0);
    }
}
