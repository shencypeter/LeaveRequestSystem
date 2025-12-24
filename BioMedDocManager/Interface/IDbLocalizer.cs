namespace BioMedDocManager.Interface
{
    public interface IDbLocalizer
    {
        string T(string key);
        string T(string key, string? cultureName);
    }
}
