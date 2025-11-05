namespace BioMedDocManager.Interface
{
    public interface ISoftDelete
    {
        DateTime? DeletedAt { get; set; }
        int? DeletedBy { get; set; }

        // (選) 顯示用
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsDeleted => DeletedAt != null;
    }

}
