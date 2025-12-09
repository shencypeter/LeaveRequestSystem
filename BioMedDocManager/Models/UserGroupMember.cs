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
        [Key][Display(Name = "群組編號")]
        public int UserGroupId { get; set; }

        /// <summary>
        /// 使用者編號
        /// </summary>
        [Display(Name = "使用者編號")]
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
