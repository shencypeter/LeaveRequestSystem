using BioMedDocManager.Helpers;
using BioMedDocManager.Interface;
using ClosedXML.Excel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
namespace BioMedDocManager.Models;


public partial class DocControlContext : DbContext
{

    IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;


    public DocControlContext(DbContextOptions<DocControlContext> options,IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DocControlContext()
    {
    }

    /// <summary>
    /// 覆蓋原本的SaveChangesAsync，自動補上新增者/新增時間、更新者/更新時間、刪除者/刪除時間
    /// </summary>
    /// <param name="ct">取消token</param>
    /// <returns></returns>
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var ctx = _httpContextAccessor.HttpContext;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    Utilities.StampCreate(entry.Entity, ctx);
                    break;

                case EntityState.Modified:
                    Utilities.StampUpdate(entry.Entity, ctx);
                    break;

                case EntityState.Deleted:
                    // 只有已實作 ISoftDelete 的Model才需要軟刪除
                    if (typeof(ISoftDelete).IsAssignableFrom(entry.Metadata.ClrType))
                    {
                        Utilities.StampDelete(entry.Entity, ctx);
                        entry.State = EntityState.Modified; // 改成更新，實現軟刪除
                    }
                    // else: 非軟刪除型別，就是真的刪除 (EntityState.Deleted)
                    break;
            }
        }

        return base.SaveChangesAsync(ct);
    }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _config.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    // ===== 系統用資料表 =====
    public virtual DbSet<AccessLog> AccessLogs { get; set; }


    // ===== 使用者、權限等資料表 =====

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }
    
    public virtual DbSet<UserGroup> UserGroups { get; set; }
    
    public virtual DbSet<UserGroupMember> UserGroupMembers { get; set; }

    public virtual DbSet<UserGroupRole> UserGroupRoles { get; set; }

    public virtual DbSet<Resource> Resources { get; set; }

    public virtual DbSet<AppAction> AppActions { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<MenuItem> MenuItems { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }


    // ===== 範例資料表 =====
    public virtual DbSet<DocControlMaintable> DocControlMaintables { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);

        // ===== 套用到所有使用ISoftDelete Interface的實體Model =====

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (typeof(ISoftDelete).IsAssignableFrom(clr))
            {
                // e => ((ISoftDelete)e).DeletedAt == null
                var param = Expression.Parameter(clr, "e");
                var body = Expression.Equal(
                    Expression.Property(Expression.Convert(param, typeof(ISoftDelete)), nameof(ISoftDelete.DeletedAt)),
                    Expression.Constant(null, typeof(DateTime?))
                );
                var lambda = Expression.Lambda(body, param);

                entityType.SetQueryFilter(lambda);
            }
        }

        // ===== 使用者、權限等資料表 =====

        modelBuilder.Entity<Department>()
        .ToTable("Department")
        .HasKey(u => u.DepartmentId);

        modelBuilder.Entity<User>()
        .ToTable("User")
        .HasKey(u => u.UserId);

        modelBuilder.Entity<Role>()
            .ToTable("Role")
            .HasKey(r => r.RoleId);

        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.ToTable("UserGroup");
            entity.HasKey(e => e.UserGroupId);
        });

        modelBuilder.Entity<UserGroupMember>(entity =>
        {
            entity.ToTable("UserGroupMember");

            // 複合主鍵
            entity.HasKey(e => new { e.UserGroupId, e.UserId });

            // 外鍵到 UserGroup
            entity.HasOne(e => e.UserGroup)
                  .WithMany(g => g.UserGroupMembers)
                  .HasForeignKey(e => e.UserGroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 外鍵到 User
            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserGroupMembers)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserGroupRole>(entity =>
        {
            entity.ToTable("UserGroupRole");

            // 複合主鍵
            entity.HasKey(e => new { e.UserGroupId, e.RoleId });

            // FK: UserGroup (CASCADE)
            entity.HasOne(e => e.UserGroup)
                  .WithMany(g => g.UserGroupRoles)
                  .HasForeignKey(e => e.UserGroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK: Role (CASCADE)
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.UserGroupRoles)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Resource>()
            .ToTable("Resource")
            .HasKey(r => r.ResourceId);

        modelBuilder.Entity<AppAction>()
            .ToTable("AppAction")
            .HasKey(r => r.AppActionId);


        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermission");

            // 複合主鍵 (RoleId, ResourceId, AppActionId)
            entity.HasKey(e => new { e.RoleId, e.ResourceId, e.AppActionId });

            // FK: Role
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK: Resource
            entity.HasOne(e => e.Resource)
                  .WithMany(res => res.RolePermissions)
                  .HasForeignKey(e => e.ResourceId)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK: AppAction
            entity.HasOne(e => e.AppAction)
                  .WithMany(act => act.RolePermissions)
                  .HasForeignKey(e => e.AppActionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("MenuItem");
            entity.HasKey(e => e.MenuItemId);

            // Self reference: Parent (NoAction 對應你的 DDL)
            entity.HasOne(e => e.Parent)
                  .WithMany(p => p.Children)
                  .HasForeignKey(e => e.MenuItemParentId)
                  .OnDelete(DeleteBehavior.NoAction);

            // Optional Resource: SetNull 對應你的 DDL
            entity.HasOne(e => e.Resource)
                  .WithMany(r => r.MenuItems)
                  .HasForeignKey(e => e.ResourceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRole");

            // 複合主鍵
            entity.HasKey(e => new { e.UserId, e.RoleId });

            // 關聯 User
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId);

            // 關聯 Role
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId);
        });

        // ===== 範例資料表 =====




        // 範例資料表：文件領用紀錄表
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

            entity.HasOne(e => e.Person)
                .WithMany()
                .HasForeignKey(e => e.Id)
                .HasPrincipalKey(u => u.UserAccount);

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
                .HasPrincipalKey(u => u.UserAccount);

            entity.HasOne(e => e.UnuseTimeModifyUser)
                .WithMany()
                .HasForeignKey(e => e.UnuseTimeModifyBy)
                .HasPrincipalKey(u => u.UserAccount);

        });
    }

    
    
}
