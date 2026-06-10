using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DominosApp.Infrastructure.Data.Models;

namespace DominosApp.Infrastructure.Data
{
    public class DominosAppDbContext : IdentityDbContext<ApplicationUser>
    {
        public DominosAppDbContext(DbContextOptions<DominosAppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Pizza> Pizzas { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderPizza> OrderPizzas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<OrderPizza>()
                .HasKey(op => new { op.OrderId, op.PizzaId });

            modelBuilder.Entity<OrderPizza>()
                .HasOne(op => op.Order)
                .WithMany(o => o.OrderPizzas)
                .HasForeignKey(op => op.OrderId);

            modelBuilder.Entity<OrderPizza>()
                .HasOne(op => op.Pizza)
                .WithMany(p => p.OrderPizzas)
                .HasForeignKey(op => op.PizzaId);

            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pizza>().HasData(
                new Pizza { Id = "pizza-1", Name = "Margherita", Price = 8.99m, Description = "Classic tomato sauce with mozzarella", ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a3/Eq_it-na_pizza-margherita_sep2005_sml.jpg/320px-Eq_it-na_pizza-margherita_sep2005_sml.jpg", IsAvailable = true },
                new Pizza { Id = "pizza-2", Name = "Pepperoni", Price = 10.99m, Description = "Loaded with pepperoni and cheese", ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a3/Eq_it-na_pizza-margherita_sep2005_sml.jpg/320px-Eq_it-na_pizza-margherita_sep2005_sml.jpg", IsAvailable = true },
                new Pizza { Id = "pizza-3", Name = "BBQ Chicken", Price = 12.99m, Description = "Grilled chicken with BBQ sauce", ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a3/Eq_it-na_pizza-margherita_sep2005_sml.jpg/320px-Eq_it-na_pizza-margherita_sep2005_sml.jpg", IsAvailable = true },
                new Pizza { Id = "pizza-4", Name = "Vegetarian", Price = 9.49m, Description = "Fresh vegetables and tomato sauce", ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a3/Eq_it-na_pizza-margherita_sep2005_sml.jpg/320px-Eq_it-na_pizza-margherita_sep2005_sml.jpg", IsAvailable = true }
            );

            var adminRoleId = "role-admin-id";
            var userRoleId = "role-user-id";
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = adminRoleId, Name = "Administrator", NormalizedName = "ADMINISTRATOR", ConcurrencyStamp = "a" },
                new IdentityRole { Id = userRoleId, Name = "User", NormalizedName = "USER", ConcurrencyStamp = "b" }
            );

            var hasher = new PasswordHasher<ApplicationUser>();
            var adminId = "admin-user-id";
            var adminUser = new ApplicationUser
            {
                Id = adminId,
                UserName = "admin@dominos.com",
                NormalizedUserName = "ADMIN@DOMINOS.COM",
                Email = "admin@dominos.com",
                NormalizedEmail = "ADMIN@DOMINOS.COM",
                FullName = "Admin User",
                Address = "Dominos HQ, Sofia",
                EmailConfirmed = true,
                SecurityStamp = "static-security-stamp",
                ConcurrencyStamp = "static-concurrency-stamp"
            };
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin123!");
            modelBuilder.Entity<ApplicationUser>().HasData(adminUser);

            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { UserId = adminId, RoleId = adminRoleId }
            );
        }
    }
}
