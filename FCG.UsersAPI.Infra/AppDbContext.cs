using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FCG.UsersAPI.Infra
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var emailConverter = new ValueConverter<Email, string>(
                email => email.Value,
                value => new Email(value));

            var passwordConverter = new ValueConverter<Password, string>(
                password => password.Value,
                value => new Password(value));

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Name).IsRequired().HasMaxLength(150);

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasConversion(emailConverter);

                entity.HasIndex(u => u.Email).IsUnique();

                entity.Property(u => u.Password)
                    .IsRequired()
                    .HasConversion(passwordConverter);
            });
        }
    }
}
