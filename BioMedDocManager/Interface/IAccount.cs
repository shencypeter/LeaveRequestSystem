using BioMedDocManager.Enums;

namespace BioMedDocManager.Interface
{
    public interface IAccount
    {
        AccountType GetAccountType();
        long GetUId(); 
        string GetAccount();        
        string GetEncryptedPassword();
    }

}
