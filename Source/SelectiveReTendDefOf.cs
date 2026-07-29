using RimWorld;
using Verse;

namespace SelectiveReTend;

[DefOf]
public static class SelectiveReTendDefOf
{
    public static JobDef JG_SelectiveReTend;

    static SelectiveReTendDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(SelectiveReTendDefOf));
    }
}
