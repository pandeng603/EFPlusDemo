using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace EFPlusDemo.Model
{
    public partial class ORMModel : DbContext
    {
        public ORMModel()
            : base("name=ORMModel")
        {
        }

        public virtual DbSet<Class> Classes { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Class>()
                .Property(e => e.CId)
                .IsUnicode(false);

            modelBuilder.Entity<Class>()
                .HasMany(e => e.Users)
                .WithRequired(e => e.Class1)
                .HasForeignKey(e => e.Class);

            modelBuilder.Entity<User>()
                .Property(e => e.UId)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.Gender)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.Class)
                .IsUnicode(false);
        }
    }
}
