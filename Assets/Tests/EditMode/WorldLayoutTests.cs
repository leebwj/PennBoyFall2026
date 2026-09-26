using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public class WorldLayoutTests
{
    private static object Load()
    {
        return MiniJson.Parse(File.ReadAllText(ColorDemoBuilder.DATA_PATH));
    }

    [Test]
    public void LayoutHasFourteenEncountersConnectorsAndAnArena()
    {
        object layout = Load();
        Assert.AreEqual(14, JsonTree.List(layout, "levels").Count);
        Assert.AreEqual(14, JsonTree.List(layout, "connectors").Count);
        Assert.IsNotNull(JsonTree.Obj(layout, "boss"));
    }

    [Test]
    public void EveryEncounterHasACheckpointAndGeometry()
    {
        foreach (object level in JsonTree.List(Load(), "levels"))
        {
            Dictionary<string, object> checkpoint = JsonTree.Obj(level, "checkpoint");
            Assert.IsNotNull(checkpoint);
            Assert.AreEqual(3, JsonTree.Floats(checkpoint, "respawn").Length);
            Assert.Greater(JsonTree.List(level, "surfaces").Count, 0);
            Assert.Greater(JsonTree.List(level, "targets").Count, 0);
        }
    }

    [Test]
    public void ParserHandlesNullsAndNestedArrays()
    {
        object tree = MiniJson.Parse("{\"c\":null,\"p\":[[1,2.5,-3e1]],\"s\":\"a\\\"b\"}");
        Assert.IsFalse(JsonTree.Has(tree, "c"));
        float[] point = JsonTree.Floats(JsonTree.List(tree, "p")[0]);
        Assert.AreEqual(-30f, point[2]);
        Assert.AreEqual("a\"b", JsonTree.Str(tree, "s"));
    }
}
