using System.Collections.Generic;

public static class RuleEngine
{
    public class TurnResult
    {
        public int p0Delta;
        public int p1Delta;
    }

    public static TurnResult Resolve(List<int> p0Played, List<int> p1Played, System.Func<int, CardSO> lookup)
    {
        var r = new TurnResult();
        int s0 = 0, s1 = 0;
        foreach (var id in p0Played) { var so = lookup(id); if (so != null) s0 += so.power; }
        foreach (var id in p1Played) { var so = lookup(id); if (so != null) s1 += so.power; }
        if (s0 > s1) r.p0Delta = 1;
        else if (s1 > s0) r.p1Delta = 1;
        return r;
    }
}
