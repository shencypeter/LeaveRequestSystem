using BioMedDocManager.Enums;
using BioMedDocManager.Models;

namespace BioMedDocManager.Interface
{
    public interface IAccessLogService
    {
        Task NewLoginFailedAsync(AccountType accountType, string accountNum, int accountId,
                                 string functionName, string actionName, int severity, string description);

        Task NewLoginSuccessAsync(User account, string functionName, string actionName);

        Task NewActionAsync(User loginAccount, string functionName, string actionName,
                            string description = "", bool isThrowError = false);

        Task NewPasswordAsync(User loginAccount, string functionName, string actionName,
                              User targetAccount);
    }

}
