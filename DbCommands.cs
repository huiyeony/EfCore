using System;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;

using static EFCore.Extension;

namespace EFCore
{
    public class DbCommands
    {
        //CRUD (CREATE READ UPDATE DELETE) 
        public static void InitializeDb(bool forceReset = false)
        {
            using (AppDbContext db = new AppDbContext())
            {
                if (!forceReset && (db.GetService<IDatabaseCreator>() as RelationalDatabaseCreator).Exists())
                    return;
                try
                {

                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                //
                CreateTestData(db);//db에 데이터 넣기

                //scalar-valued-function 등록
                string commands =
                                @"
                                create function CalcAverage (@itemId int) returns float
                                as

                                begin

                                declare @result as float

                                select @result = avg(cast([Score] as float))
                                from ItemReview as r
                                where r.ItemId = @itemId

                                return @result

                                end
                                ";
               

                db.Database.ExecuteSqlRaw(commands);//SQL 명령어 실행 
            

                Console.WriteLine("Initialized Db");
            }

        }
        public static void CreateTestData(AppDbContext db)//db에 테스트할 테이터 만들기 
        {

            Player usr1 = new Player() { Name = "User@1" };
            Player usr2 = new Player() { Name = "User@2" };
            Player usr3 = new Player() { Name = "User@3" };

            var items = new List<Item>()//PK는 db가 자동 생성 해줌 
            {
                new Item()
                {
                    TemplateId = 101,
                    CreateDate = DateTime.Now,
                    Owner = usr1

                },
                new Item()
                {
                    TemplateId = 102,
                    CreateDate = DateTime.Now,
                    Owner = usr2,
                },
                new Item()
                {
                    TemplateId = 103,
                    CreateDate = DateTime.Now,
                    Owner = usr3
                }

            };
            //별점 추가 
            items[0].Reviews = new List<ItemReview>()
            {
                new ItemReview(){Score = 5},
                new ItemReview(){Score = 4},
                new ItemReview(){Score = 3},
                new ItemReview(){Score = 2}
            };
            items[1].Reviews = new List<ItemReview>()
            {
                new ItemReview(){Score = 5},
                new ItemReview(){Score = 4},
                new ItemReview(){Score = 3},
                new ItemReview(){Score = 2}
            };
            //

            Guild guild = new Guild()
            {
                GuildName = "T1",
                Members = new List<Player>() { usr1, usr2, usr3 }
            };

            db.Items.AddRange(items);
            db.Guilds.Add(guild);

            try
            {

                db.SaveChanges();//db에 저장 
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }

        }
        //오늘의 주제 : Foreign key와 nullable
        // Dependent data가  Principal data 없이 독립적으로 존재 할 수 있을까 ?
        // -1)주인이 없는 아이템은 불가능
        // -2)주인이 없는 아이템도 가능 !
        // 어떻게 구분해서 설정할까 ?
        //- 답은 Nullable !
        public static void ShowItems()//db에 있는 정보 읽어 오기
        {
            using (AppDbContext db = new AppDbContext())//db에 연결 
            {
                //AsNoTracking->ReadOnly
                //include-> Eager Loading(즉시 로딩)
                foreach (Item item in db.Items.AsNoTracking().Include(i => i.Owner))
                {

                    if(item.Owner == null)
                        Console.WriteLine($"TemplateId({item.TemplateId}) CreateDate({item.CreateDate}) Owner(0)");
                    else
                        Console.WriteLine($"TemplateId({item.TemplateId}) CreateDate({item.CreateDate}) Owner({item.Owner.Name}) OwnerId({item.Owner.PlayerId})");
                }
            }

        }
        public static void CalcAverageReviewScore()
        {
            using(AppDbContext db = new AppDbContext())
            {
                try
                {
                    foreach (double? avg in db.Items.Select(i => Program.CalcAverage(i.ItemId)))
                    {
                        if (avg == null)
                            Console.WriteLine("No Review!");
                        else
                            Console.WriteLine($"AverageReviewScore({avg})");
                    }
                }catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }


            }
        }
        public static void NullableTest()
        {
            ShowItems();

            //Q 플레이어가 삭제 될때 아이템은 어떻게 될까 ?
            Console.WriteLine("Enter playerid (to delete)");
            Console.Write(">");

            int id = int.Parse(Console.ReadLine());//문자열 -> int 으로 파싱

            using(AppDbContext db = new AppDbContext())
            {
                Player player = db.Players
                                    .Include(p => p.OwnedItem) // 웬만하면 join 하자 
                                    .Single(p => p.PlayerId == id);
                db.Players.Remove(player);

                db.SaveChanges();
            }

            ShowItems(); 
        }
        //1v1 Relationship -> update !
        public static void Update_1v1()
        {
            ShowItems();

            //Q 플레이어가 아이템을 바꾸면 기존의 아이템은 어떻게 될까 ? 
            Console.WriteLine("Enter playerid (to update item)");
            Console.Write(">");
            int id = int.Parse(Console.ReadLine());//문자열 -> int 으로 파싱

            using (AppDbContext db = new AppDbContext())
            {

                var player = db.Players.Include(p => p.OwnedItem)
                           .Single(p => p.PlayerId == id);

                player.OwnedItem = new Item() { TemplateId = 888, CreateDate = DateTime.Now };//아이템 바꾸기

                db.SaveChanges();

            }

            ShowItems();


            }

        public static void ShowGuilds()//길드Dto 정보 출력 
        {
            using (AppDbContext db = new AppDbContext())
            {
                foreach (var guild in db.Guilds.GuildToDto())
                {
                    Console.WriteLine($"GuildId({guild.GuildId}) GuildName({guild.Name}) MemberCount({guild.MemberCount})");
                }
            }
        }
        //1vM Relationship -> update ! 
        public static void Update_1vM()
        {
            ShowGuilds();

            //Q 길드가 길드원을 바꾸면 기존의 길드원들은 어떻게 될까 ? 
            Console.WriteLine("Enter guildId (to update guildMembers)");
            Console.Write(">");
            int id = int.Parse(Console.ReadLine());//문자열 -> int 으로 파싱

            using (AppDbContext db = new AppDbContext())
            {

                var guild = db.Guilds.Include(g => g.Members)
                           .Single(g => g.GuildId == id);


                guild.Members.Add(new Player() { Name = "User@4" });

                db.SaveChanges();

            }

            ShowGuilds();


        }
        //데이터의 소프트 삭제
        public static void DeleteItem()
        {
            ShowItems();

            Console.WriteLine("Select Delete ItemId");
            Console.Write(">");
            int id = int.Parse(Console.ReadLine());//문자열 -> int 으로 파싱

            using (AppDbContext db = new AppDbContext())
            {
                Item item = db.Items.Find(id);

                item.SoftDeleted = true;

                db.SaveChanges();

            }

            ShowItems();
        }
    }
}

