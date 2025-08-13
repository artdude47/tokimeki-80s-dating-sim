using NUnit.Framework;
using Game.Domain.Relationships;

public class RelationshipTests
{
    [Test]
    public void Set_And_Add_Affection_Work()
    {
        var rels = new RelationshipState();
        rels.SetInitial("npc_ash", 10);
        Assert.AreEqual(10, rels.GetAffection("npc_ash"));
        var v = rels.AddAffection("npc_ash", 5);
        Assert.AreEqual(15, v);
        Assert.AreEqual(15, rels.GetAffection("npc_ash"));
    }

    [Test]
    public void Snapshot_And_Restore_Roundtrip()
    {
        var rles = new RelationshipState();
        rles.SetInitial("npc_jen", 20);
        var snap = rles.Snapshot();
        var rels2 = new RelationshipState();
        rels2.Restore(snap);
        Assert.AreEqual(20, rels2.GetAffection("npc_jen"));
    }
}
