using Microsoft.EntityFrameworkCore;
using SantiyeTalepApi.Models;

namespace SantiyeTalepApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<Offer> Offers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configurations
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.FullName).HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(20);
            });

            // Employee configurations
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.Position).HasMaxLength(100);
                
                entity.HasOne(e => e.User)
                    .WithOne(u => u.Employee)
                    .HasForeignKey<Employee>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Site)
                    .WithMany(s => s.Employees)
                    .HasForeignKey(e => e.SiteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Supplier configurations
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.CompanyName).HasMaxLength(255);
                entity.Property(e => e.TaxNumber).HasMaxLength(50);
                entity.Property(e => e.Address).HasMaxLength(500);
                
                entity.HasOne(s => s.User)
                    .WithOne(u => u.Supplier)
                    .HasForeignKey<Supplier>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Site configurations
            modelBuilder.Entity<Site>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(1000);
            });

            // Request configurations
            modelBuilder.Entity<Request>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(2000);
                
                entity.HasOne(r => r.Employee)
                    .WithMany(e => e.Requests)
                    .HasForeignKey(r => r.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(r => r.Site)
                    .WithMany(s => s.Requests)
                    .HasForeignKey(r => r.SiteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Offer configurations
            modelBuilder.Entity<Offer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).HasMaxLength(1000);
                
                entity.HasOne(o => o.Request)
                    .WithMany(r => r.Offers)
                    .HasForeignKey(o => o.RequestId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(o => o.Supplier)
                    .WithMany(s => s.Offers)
                    .HasForeignKey(o => o.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Admin user - admin123 şifresi için BCrypt hash
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Email = "admin@santiye.com",
                    // admin123 şifresi için doğru BCrypt hash
                    Password = "$2a$11$V070yifCQwjKA5g1Ag/FHeqNHjWyUZTZC.cE3Q3nZueVTUr4up4x.",
                    FullName = "Sistem Yöneticisi",
                    Phone = "555-000-0000",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
            
            // Sample site
            modelBuilder.Entity<Site>().HasData(
                new Site
                {
                    Id = 1,
                    Name = "Ana Şantiye",
                    Address = "İstanbul, Türkiye",
                    Description = "Ana şantiye lokasyonu",
                    IsActive = true
                },
                new Site
                {
                    Id = 2,
                    Name = "Ankara Şantiye",
                    Address = "Ankara, Türkiye",
                    Description = "İkinci şantiye lokasyonu",
                    IsActive = true
                }
            );
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Warning'i bastır
                optionsBuilder.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            }
        }
    }
}
