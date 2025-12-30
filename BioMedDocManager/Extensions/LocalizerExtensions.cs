using BioMedDocManager.Interface;

public static class LocalizerExtensions
{
    public static T WithLoc<T>(this T entity, IDbLocalizer loc) where T : class, IHasDbLocalizer
    {
        entity.Loc = loc;
        return entity;
    }

    public static IEnumerable<T> WithLoc<T>(this IEnumerable<T> list, IDbLocalizer loc) where T : class, IHasDbLocalizer
    {
        foreach (var x in list) x.Loc = loc;
        return list;
    }
}
