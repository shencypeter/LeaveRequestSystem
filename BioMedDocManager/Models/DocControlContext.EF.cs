using Microsoft.EntityFrameworkCore;

namespace BioMedDocManager.Models
{

    /// <summary>
    /// 這一半用來寫 EF Core 實體類別
    /// </summary>
    public partial class DocControlContext : DbContext
    {

        IConfiguration _config;

        public DocControlContext(DbContextOptions<DocControlContext> options)
            : base(options)
        {
        }

        public DocControlContext()
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = _config.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlServer(connectionString);
            }
        }


        public virtual DbSet<User> Users { get; set; }

        public virtual DbSet<Role> Roles { get; set; }

        public virtual DbSet<UserRole> UserRoles { get; set; }

        public virtual DbSet<DocControlMaintable> DocControlMaintables { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<User>()
            .ToTable("user")
            .HasKey(u => u.Id);

            modelBuilder.Entity<Role>()
                .ToTable("role")
                .HasKey(r => r.Id);

            modelBuilder.Entity<UserRole>()
                .ToTable("user_role")
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<DocControlMaintable>(entity =>
            {
                entity.HasKey(e => e.IdNo);

                entity.ToTable("doc_control_maintable");

                entity.Property(e => e.IdNo)
                    .HasMaxLength(50)
                    .HasComment("文件編號")
                    .HasColumnName("id_no");
                entity.Property(e => e.DateTime).HasColumnName("date_time");

                entity.Property(e => e.DocVer)
                    .HasMaxLength(50)
                    .HasComment("版本")
                    .HasColumnName("doc_ver");

                entity.Property(e => e.Id)
                    .HasMaxLength(50)
                    .HasComment("工號")
                    .HasColumnName("id");

                entity.Property(e => e.InTime).HasColumnName("in_time");

                entity.Property(e => e.Name)
                    .HasComment("紀錄名稱")
                    .HasColumnName("name");

                entity.Property(e => e.OriginalDocNo)
                    .HasComment("表單編號")
                    .HasColumnName("original_doc_no");
                /*
                entity.Property(e => e.PersonName)
                    .HasMaxLength(50)
                    
                    .HasComment("領用人")
                    .HasColumnName("person_name");
                */

                entity.HasOne(e => e.Person)
                    .WithMany()
                    .HasForeignKey(e => e.Id)
                    .HasPrincipalKey(u => u.UserName);

                entity.Property(e => e.ProjectName)
                    .HasMaxLength(50)
                    .HasComment("專案代碼")
                    .HasColumnName("project_name");

                entity.Property(e => e.Purpose)
                    .HasComment("目的")
                    .HasColumnName("purpose");

                entity.Property(e => e.RejectReason)
                    .HasComment("註銷原因")
                    .HasColumnName("reject_reason");

                entity.Property(e => e.Type)
                    .HasMaxLength(50)
                    .HasComment("文件類別")
                    .HasColumnName("type");

                entity.Property(e => e.UnuseTime).HasColumnName("unuse_time");

                entity.Property(e => e.IsConfidential)
                   .HasColumnName("is_confidential")
                   .HasComment("是否機密");

                entity.Property(e => e.IsSensitive)
                    .HasColumnName("is_sensitive")
                    .HasComment("是否機敏");

                entity.Property(e => e.InTimeModifyAt)
                    .HasColumnType("datetime")
                    .HasColumnName("in_time_modify_at")
                    .HasComment("入庫時間異動時間");

                entity.Property(e => e.UnuseTimeModifyAt)
                    .HasColumnType("datetime")
                    .HasColumnName("unuse_time_modify_at")
                    .HasComment("註銷時間異動時間");

                entity.Property(e => e.InTimeModifyBy)
                    .HasMaxLength(50)
                    .IsUnicode(true)
                    .HasComment("入庫時間異動人員")
                    .HasColumnName("in_time_modify_by");

                entity.Property(e => e.UnuseTimeModifyBy)
                    .HasMaxLength(50)
                    .IsUnicode(true)
                    .HasComment("註銷時間異動人員")
                    .HasColumnName("unuse_time_modify_by");

                entity.HasOne(e => e.InTimeModifyUser)
                    .WithMany()
                    .HasForeignKey(e => e.InTimeModifyBy)
                    .HasPrincipalKey(u => u.UserName);

                entity.HasOne(e => e.UnuseTimeModifyUser)
                    .WithMany()
                    .HasForeignKey(e => e.UnuseTimeModifyBy)
                    .HasPrincipalKey(u => u.UserName);

            });

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    }
}
