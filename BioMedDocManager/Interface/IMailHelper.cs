namespace BioMedDocManager.Interface
{
    public interface IMailHelper
    {
        Task SendMailAsync(
            string subject,
            string body,
            List<string>? toGroup,
            List<string>? ccGroup,
            List<string>? bccGroup,
            List<string>? attachmentGroup,
            bool isBodyHtml = true);
    }

}
