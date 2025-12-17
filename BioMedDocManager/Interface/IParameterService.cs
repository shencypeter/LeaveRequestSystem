using BioMedDocManager.Enums;
using BioMedDocManager.Models;

namespace BioMedDocManager.Interface
{
    public interface IParameterService
    {
        string? GetString(string code);
        int? GetInt(string code);
        bool GetBool(string code);
        T? GetJson<T>(string code);
    }
}
