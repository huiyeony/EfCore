using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace EFCore
{
    public class AppDbContext :DbContext
    {
        //테이블 정보 
        public DbSet<Item> Items { get; set; }
        //상속 받은 테이블 -> Db?
        //
        public DbSet<Player> Players { get; set; }
        public DbSet<Guild> Guilds { get; set; }


        string connection = "Server =tcp:127.0.0.1,1433; Database=EfCore; User =sa; Password=6ehd809gh!!!; TrustServerCertificate=true;";

        protected override void OnConfiguring(DbContextOptionsBuilder option)
        {
            option.UseSqlServer(connection);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            //softDeleted 가 true 이면 무시한다 
            builder.Entity<Item>()
                .HasQueryFilter(i => i.SoftDeleted == false);

            //Db에 함수 등록
            builder.HasDbFunction(() => Program.CalcAverage(0));

            //builder.Entity<Item>()
            //    .Property("CreateDate")
            //    .HasDefaultValue(new DateTime(2023, 5, 1));

            //saveChanges 호출 !

            //builder.Entity<Item>()
            //    .Property("CreateDate")
            //    .HasDefaultValueSql("GETDATE"); //SQL FRAGMENT

            builder.Entity<Item>()
                .Property("CreateDate")
                .HasValueGenerator((p, e) => { return new DateGenerator(); });

        }

        private class DateGenerator : ValueGenerator<string>
        {
            public override bool GeneratesTemporaryValues => throw new NotImplementedException();

            public override string Next(EntityEntry entry)
            {
                return "DATETIME" + DateTime.Now;
            }
        }
    }
}