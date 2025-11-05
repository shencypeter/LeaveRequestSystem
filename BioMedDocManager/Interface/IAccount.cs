using BioMedDocManager.Enums;

namespace BioMedDocManager.Interface
{
    public interface IAccount
    {
        AccountType GetAccountType();
        int GetUId(); 
        string GetAccount();        
        string GetEncryptedPassword();
    }

}
