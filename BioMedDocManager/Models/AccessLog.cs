using BioMedDocManager.Enums;
using BioMedDocManager.Helpers;
using Microsoft.Identity.Client;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioMedDocManager.Models
{
    /// <summary>
    /// 資料庫：AccessLog 資料表
    /// </summary>
    public partial class AccessLog
    {
        [Key]
        public System.Guid LogId { get; set; }
        public System.DateTime LogDateTime { get; set; }
        public System.DateTime LogDateTimeUtc { get; set; }
        public int AccessLogType { get; set; }
        public Nullable<int> AccountType { get; set; }
        public string AccountNum { get; set; }
        public int AccountId { get; set; }
        public string ClientIp { get; set; }
        public string RequestMethod { get; set; }
        public string RequestUrl { get; set; }
        public string RequestReferrer { get; set; }
        public string FunctionName { get; set; }
        public string ActionName { get; set; }
        public Nullable<bool> IsSuccess { get; set; }
        public Nullable<int> Severity { get; set; }
        public string Description { get; set; }
    }

}
