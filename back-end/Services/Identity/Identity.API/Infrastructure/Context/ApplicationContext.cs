using Commom.Domain.PasswordHasher;
using Commom.Infra.Base;
using Identity.API.Application.Entities;
using Identity.API.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Infrastructure.Context
{
    public class ApplicationContext : BaseContext
    {
        private readonly IPasswordHasherService _passwordHasher;

        public DbSet<User> Users { get; set; }
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

        public ApplicationContext(DbContextOptions<ApplicationContext> options, IPasswordHasherService passwordHasher) : base(options)
        {
            _passwordHasher= passwordHasher;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserMap());
            modelBuilder.ApplyConfiguration(new UserRefreshTokenMap());

            modelBuilder
                .Entity<User>()
                .HasData(new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    Password = _passwordHasher.HashPassword("admin"),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Role = "admin"
                });
        }
    }
}
