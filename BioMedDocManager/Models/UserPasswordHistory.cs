using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models
{
    /// <summary>
    /// 使用者密碼歷史表
    /// </summary>

    public class UserPasswordHistory
    {
        public long UserPasswordHistoryId { get; set; }

        public long UserId { get; set; }

        [Required]
        [MaxLength(512)]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public long? CreatedBy { get; set; }

        // 導覽屬性
        public virtual User User { get; set; } = null!;

    }
}
