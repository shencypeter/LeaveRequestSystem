using BioMedDocManager.Interface;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models
{
    /// <summary>
    /// 使用者群組成員
    /// </summary>
    public class UserGroupMember
    {
        /// <summary>
        /// 群組編號
        /// </summary>
        [Key]
        [Display(Name = "UserGroupMember.UserGroupId")]
        public int UserGroupId { get; set; }

        /// <summary>
        /// 使用者編號
        /// </summary>
        [Key]
        [Display(Name = "UserGroupMember.UserId")]
        public int UserId { get; set; }

        /// <summary>
        /// 群組
        /// </summary>
        public UserGroup? UserGroup { get; set; }

        /// <summary>
        /// 使用者
        /// </summary>
        public User? User { get; set; }
    }
}
