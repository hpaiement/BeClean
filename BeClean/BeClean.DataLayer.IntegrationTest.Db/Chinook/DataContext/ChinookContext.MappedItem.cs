using BeClean.DataLayer.IntegrationTest.Db.Chinook.Models;
using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.DataContext;

public partial class ChinookContext
{
    public virtual DbSet<MappedItem> MappedItem { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MappedItem>(entity =>
        {
            entity.ToTable("mapped_item", "bulk_test");

            entity.HasIndex(e => e.Code).IsUnique();

            entity.Property(e => e.Id).HasColumnName("item_id").ValueGeneratedNever();
            entity.Property(e => e.Code).HasColumnName("item_code").HasMaxLength(20);
            entity.Property(e => e.Label).HasColumnName("label_text").HasMaxLength(120);
            entity.Property(e => e.Position).HasColumnName("Order");
            entity.Property(e => e.Batch).HasColumnName("import_batch");
        });
    }
}
