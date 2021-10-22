using Fake.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Fake.API.Database
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<TouristRoute> TouristRoutes { get; set; }
        public DbSet<TouristRoutePicture> TouristRoutePictures { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<LineItem> LineItems { get; set; }
        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<TouristRoute>().HasData(new TouristRoute()
            //{
            //    Id = Guid.NewGuid(),
            //    Title = "ceshititle",
            //    Description = "shuoming",
            //    OriginPrice = 0,
            //    CreateTime = DateTime.UtcNow
            //});

            //假資料
            var touristRouteJsonData = File.ReadAllText(@"C:/西瓜瓜\學習/DotNetCore_WebAPI/Fake.API/Fake.API/Database/touristRoutesMockData.json");
            IList<TouristRoute> touristRoutes = JsonConvert.DeserializeObject<IList<TouristRoute>>(touristRouteJsonData);
            modelBuilder.Entity<TouristRoute>().HasData(touristRoutes);

            var touristRoutePictureJsonData = File.ReadAllText(@"C:/西瓜瓜\學習/DotNetCore_WebAPI/Fake.API/Fake.API/Database/touristRoutePicturesMockData.json");
            IList<TouristRoutePicture> touristPictureRoutes = JsonConvert.DeserializeObject<IList<TouristRoutePicture>>(touristRoutePictureJsonData);
            modelBuilder.Entity<TouristRoutePicture>().HasData(touristPictureRoutes);

            //初始化用戶與角色的種子數據
            //1. 更新用戶與角色的外鍵
            modelBuilder.Entity<ApplicationUser>(u =>
                u.HasMany(x => x.UserRoles)
                .WithOne().HasForeignKey(ur => ur.UserId).IsRequired()
            );

            //2. 添加管理員角色
            var adminRoleId = "308660dc-ertyjktjrthegrwefwdwfg4h5j6r";
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole()
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    NormalizedName = "Admin".ToUpper()
                }    
            );

            //3. 添加用戶
            var adminUserId = "asdfg";
            ApplicationUser adminUser = new ApplicationUser {
                Id = adminUserId,
                UserName = "Alice@mail.com",
                NormalizedUserName = "Alice@mail.com".ToUpper(),
                TwoFactorEnabled = false,
                EmailConfirmed = true,
                PhoneNumber = "1234567",
                PhoneNumberConfirmed = false
            };

            var ph = new PasswordHasher<ApplicationUser>();
            adminUser.PasswordHash = ph.HashPassword(adminUser, "Fake123$");
            modelBuilder.Entity<ApplicationUser>().HasData(adminUser);

            //4.給用戶加入管理員角色
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
               new IdentityUserRole<string>()
               {
                   RoleId = adminRoleId,
                   UserId = adminUserId
               }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}
