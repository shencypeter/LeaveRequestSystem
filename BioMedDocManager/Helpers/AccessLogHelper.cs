using BioMedDocManager.Enums;
using BioMedDocManager.Interface;
using BioMedDocManager.Models;
using Microsoft.AspNetCore.Http.Extensions;

namespace BioMedDocManager.Helpers
{
    /// <summary>
    /// 紀錄連線log
    /// </summary>
    public sealed class AccessLogService : IAccessLogService
    {
        private readonly DocControlContext _context;
        private readonly IHttpContextAccessor _http;

        public AccessLogService(DocControlContext context, IHttpContextAccessor http)
        {
            _context = context;
            _http = http;
        }

        public async Task NewLoginFailedAsync(
            AccountType accountType,
            string accountNum,
            int accountId,
            string functionName,
            string actionName,
            int severity,
            string description)
        {
            var log = GenerateBaseAccessLog(AccessLogType.LoginLog, functionName, actionName);
            log.AccountType = (int)accountType;
            log.AccountNum = accountNum;
            log.AccountId = accountId;
            log.IsSuccess = false;
            log.Severity = severity;
            log.Description = description;

            await _context.AccessLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task NewLoginSuccessAsync(User account, string functionName, string actionName)
        {
            var log = GenerateBaseAccessLog(AccessLogType.LoginLog, functionName, actionName);
            log.AccountType = (int)account.GetAccountType();
            log.AccountNum = account.GetAccount();
            log.AccountId = account.GetUId();
            log.IsSuccess = true;
            log.Severity = 0;

            await _context.AccessLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task NewActionAsync(
            User? loginAccount,
            string functionName,
            string actionName,
            string description = "",
            bool isThrowError = false)
        {
            try
            {
                if (loginAccount == null)
                {
                    Exception ex = new Exception("新增操作紀錄時，登入帳號為null");
                    Utilities.WriteExceptionIntoLogFile("NewActionAsync，loginAccount為null", ex);
                    throw ex;
                }

                var log = GenerateBaseAccessLog(AccessLogType.ActionLog, functionName, actionName);
                log.AccountType = (int)loginAccount.GetAccountType();
                log.AccountNum = loginAccount.GetAccount();
                log.AccountId = loginAccount.GetUId();
                log.IsSuccess = true;
                log.Severity = 0;
                log.Description = description;

                await _context.AccessLogs.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Utilities.WriteExceptionIntoLogFile("新增一筆操作紀錄 NewActionAsync() 的 Exception", ex);
                if (isThrowError) throw; // 保留原 StackTrace
            }
        }

        public async Task NewPasswordAsync(
            User loginAccount,
            string functionName,
            string actionName,
            User targetAccount)
        {
            var log = GenerateBaseAccessLog(AccessLogType.PasswordLog, functionName, actionName);
            log.AccountType = (int)loginAccount.GetAccountType();
            log.AccountNum = loginAccount.GetAccount();
            log.AccountId = loginAccount.GetUId();
            log.IsSuccess = true;
            log.Severity = 0;
            log.Description = $"目標帳號類型：{(int)targetAccount.GetAccountType()}，目標帳號：{targetAccount.GetAccount()}，目標id：{targetAccount.GetUId()}，目的：設定新密碼";

            await _context.AccessLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public List<AccessLog> GetMemberLoginLogs(IAccount account, DateTime fromDateTime)
        {
            int accountType = (int)account.GetAccountType();
            int accountId = account.GetUId();

            return _context.AccessLogs
                .Where(w => w.LogDateTime >= fromDateTime &&
                            w.AccessLogType == (int)AccessLogType.LoginLog &&
                            w.AccountType == accountType &&
                            w.AccountId == accountId)
                .ToList();
        }

        private AccessLog GenerateBaseAccessLog(AccessLogType accessLogType, string functionName, string actionName)
        {
            var nowUtc = DateTimeOffset.UtcNow;

            var ctx = _http.HttpContext;
            var req = ctx?.Request;

            string clientIp = string.Empty;
            string requestMethod = req?.Method ?? string.Empty;
            string requestUrl = req?.GetDisplayUrl() ?? string.Empty;
            string requestReferrer = req?.Headers["Referer"].ToString() ?? string.Empty;
            string userAgent = req?.Headers["User-Agent"].ToString() ?? string.Empty;
            string xff = req?.Headers["X-Forwarded-For"].ToString() ?? string.Empty;
            string traceId = ctx?.TraceIdentifier ?? string.Empty;

            if (req != null)
            {
                clientIp = Utilities.GetClientIpAddress(req);
                if (string.IsNullOrWhiteSpace(clientIp) && !string.IsNullOrWhiteSpace(xff))
                    clientIp = xff.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            }

            return new AccessLog
            {
                LogId = Guid.NewGuid(),
                LogDateTimeUtc = nowUtc.UtcDateTime,
                LogDateTime = nowUtc.LocalDateTime,
                AccessLogType = (int)accessLogType,
                ClientIp = clientIp,
                RequestMethod = requestMethod,
                RequestUrl = requestUrl,
                RequestReferrer = requestReferrer,
                FunctionName = functionName,
                ActionName = actionName,
                Severity = 0,
            };
        }

    }
}
