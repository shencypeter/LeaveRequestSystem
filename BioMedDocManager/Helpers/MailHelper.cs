using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Linq;

public class MailHelper(IParameterService _param) : IMailHelper
{

    /// <summary>
    /// 使用 SMTP 寄送郵件（帳號與應用程式密碼從Parameter表取得）
    /// </summary>
    public async Task SendMailAsync(
        string subject,
        string body,
        List<string>? toGroup,
        List<string>? ccGroup,
        List<string>? bccGroup,
        List<string>? attachmentGroup,
        bool isBodyHtml = true)
    {
        // === 從 Parameter 抓寄件設定 ===
        // EMAIL_ACCOUNT：Gmail 帳號，例如 OOO@gmail.com
        // EMAIL_APP_PASSWORD：SMTP 密碼 / Google 應用程式密碼（16 碼）
        var Account = _param.GetString("EMAIL_ACCOUNT")?.Trim();
        var AppPassword = _param.GetString("EMAIL_APP_PASSWORD")?.Trim();
        var SmtpHost = _param.GetString("EMAIL_SMTP_HOST")?.Trim();
        int SmtpPort = _param.GetInt("EMAIL_SMTP_PORT") ?? 587;// 預設 587

        if (string.IsNullOrWhiteSpace(Account) || string.IsNullOrWhiteSpace(AppPassword) || string.IsNullOrWhiteSpace(SmtpHost))
        {
            throw new InvalidOperationException("寄信設定錯誤：EMAIL_ACCOUNT / EMAIL_APP_PASSWORD 或 EMAIL_SMTP_HOST 未在 Parameter 表設定。");
        }

        // 顯示名稱可以用 SITE_NAME，若沒有就用帳號
        var SiteName = _param.GetString("SITE_NAME") ?? "範例網站";
        
        using (var msg = new MailMessage())
        {
            // 寄件者
            msg.From = new MailAddress(Account, SiteName, Encoding.UTF8);

            // 收件者
            if (toGroup != null)
            {
                foreach (var to in toGroup.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    msg.To.Add(to.Trim());
                }
            }

            // 副本
            if (ccGroup != null)
            {
                foreach (var cc in ccGroup.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    msg.CC.Add(cc.Trim());
                }
            }

            // 密件副本
            if (bccGroup != null)
            {
                foreach (var bcc in bccGroup.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    msg.Bcc.Add(bcc.Trim());
                }
            }

            if (msg.To.Count == 0 && msg.CC.Count == 0 && msg.Bcc.Count == 0)
            {
                string customErrorString = "SendMailAsync 需至少指定一個收件人（任一 To / Cc / Bcc）。";
                InvalidOperationException ex = new InvalidOperationException(customErrorString);
                Utilities.WriteExceptionIntoLogFile(customErrorString, ex);
                throw ex;
            }

            msg.Subject = subject ?? string.Empty;
            msg.SubjectEncoding = Encoding.UTF8;
            msg.Body = body ?? string.Empty;
            msg.BodyEncoding = Encoding.UTF8;
            msg.IsBodyHtml = isBodyHtml;

            // 附件（傳入的是檔案完整路徑）
            if (attachmentGroup != null)
            {
                foreach (var path in attachmentGroup.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    try
                    {
                        if (System.IO.File.Exists(path))
                        {
                            msg.Attachments.Add(new Attachment(path));
                        }
                        else
                        {
                            string customErrorString = $"SendMailAsync 附件檔案不存在：{path}";
                            Utilities.WriteExceptionIntoLogFile(customErrorString, new Exception(customErrorString));
                        }
                    }
                    catch (Exception ex)
                    {
                        string customErrorString = $"新增附件時發生錯誤：{path}";
                        Utilities.WriteExceptionIntoLogFile(customErrorString, ex);
                    }
                }
            }

            using (var client = new SmtpClient())
            {
                client.Host = SmtpHost;
                client.Port = SmtpPort;
                client.EnableSsl = true; // 固定啟用 SSL
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(Account, AppPassword);
                client.Timeout = 1000 * 30; // 30 秒

                try
                {
                    await client.SendMailAsync(msg);
                }
                catch (SmtpException smtpEx)
                {
                    string customErrorString = $"寄送 Email 失敗，Subject={subject}";
                    Utilities.WriteExceptionIntoLogFile(customErrorString, smtpEx);
                    throw;
                }
            }
        }
    }
}
