using System;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using static EFCore.Extension;

//데이터 모델링
//1 : 1
//1 : 다
//다 : 다
// (child /dependent)  -> FK 들고 있음


//오늘의 주제 : Configuration(A->B->C)
//A. Convention 
//- 각종 형식과 이름을 규칙에 맞게 만들면, Ef Core에서 알아서 처리
//- 쉽고 빠르지만 , 모든 경우를 처리 할 수 는 없다~ 
//B. Data Annotation 
// - class/property 등에 Attribute를 붙여 추가 정보 
//C. Fluent Api(직접 정의 )
// 
// - OnModelCreating 에서 그냥 직접 설정을 정의해서 만드는 귀찮은 방식 
// - 하지만 활용범위는 가장 넓다

//-------- CONVENTION -----------------------------------------
//1) Entity class 관련 
//- public 접근 한정자 + non-static
//- property 중에서 public getter 찾으면서 분석 함
//- property 이름 = table column 이름
//1) 이름 ,형식 ,크기 관련
// - .NET 형식 <-> SQL 형식 (int ,bool..)
// - .NET 형식의 Nullable 여부를 따라감 (string은 nullable, int 는 non-nullable) 
//3) PK 관련
//- Id 혹은 <클래스 이름>Id 를 PK으로 인정 함(후자 권장 ) 
//- 복합키(composite Key) 는 convention으로 처리 불가
//----------------------------------------------------------------

//Q1) DB column type,size, nullable
//Nullable   [Required]      .IsRequired()
//문자열 길이  [MaxLength(20)]  .HasMaxLength()
//문자 형식                     .IsUnicode()

//Q2) PK
//[Key][Column(Order = 0)] [Key][Column(Order = 1)]
//.HasKey(x=> new {x.Prop1 , x.Prop2 })

//Q3) Index
//인덱스 추가                     .HasIndex(p=>p.Prop1)
//복합 인덱스 추가                 .HasIndex(p=>new {p.Prop1, p.Prop2}
//인덱스 이름을 정해서 추가          .HasIndex(p=>p.Prop1).HasName("index_Prop1")
//유니크 인덱스 추가                .HasIndex(p=>p.Prop1).IsUnique()

//Q4) 테이블 이름
//DBSet<T> property 이름 or class 이름 
//[Table("MyTable")]            .ToTable("MyTable")

//Q5) column이름
//property 이름
//[Column("MyCol")]              .HasColumnName("MyCol")

//Q6)코드 모델링 에서는 사용하되 DB 모델링 에서는 제외하고 싶을 때 (property + class 모두 해당 )
//[NotMapped]                    .Ignore()

//Q7) soft delete
//                               .HasQueryFilter()

//기본 용어 복습
// 1) Principal Entity
// 2) Dependent Entity
// 3) Navigational Property (FK..)
// 4) Primary Key(PK)
// 5) Foreign Key(FK)
// 6) Principal Key (PK or Unique Alternative Key)
// 7) Required Relationship (non-null)
// 8) Optional Relationship (Nullable)

//convention을 이용한 FK 설정
// 1) <PrincipalKeyName>          PlayerId
// 2) <Class><PrincipalKeyName>   PlayerPlayerId
// 3) <NavigationalPropertyName>  OwnerPlayerId or OwnerId

// FK와 Nullable
// 1) Required Relationship (not-null)
// ->principal 데이터 삭제하면 Dependent도 삭제됨
// 2) Optional Relationship
// ->Principal 삭제할때 Dependent Tracking하고 있으면 FK null세팅
// ->Principal 삭제할때 Dependent Tracking하고 있지 않으면 Exception !

// convention 방식으로 못하는 것들
// 1) 복합 FK
// 2) 다수의 Navigational Property가 같은 class를 참조할 때
// ---- Data Annotation 으로 Relation 설정 ------
// [ForeignKey("prop1")]
// prop1 : Name of Navigational Property or Name of FK
// [InverseProperty("prop1")]
// -> principal data에 추가 !!
//
// ----Fluent Api 으로 Relation 설정 -------
//.HasOne()      .HasMany()
//.WithOne()     .WithMany()
//.HasForeignKey() .IsRequired()   .OnDelete()
//.HasConstraintName()   .HasPrincipalKey()

// 3) DB나 삭제 관련 커스터마이징이 필요할 때
// ->  Data Annotation으로 Relationship 설정


//오늘의 주제 : shadow Property + Backing Field
//Shadow Property 
//Class에는 있지만 DB에는 없음 -> [NotMapped] ,Ignore()
//DB에는 있지만 Class에는 없음 
//생성 ->  .Property<DateTime>("RecoverdDate")
//Read / Write -> .Property("RecoveredData").CurrentValue
//Backing Field
//private Field를 DB에 매핑하고 public getter 로 가공해서 사용
//예 ) Db에서는 json string 형태로 저장하고 , 클라쪽에서는 json string을 객체로 사용하고 싶을 때
//일반적으로 Fluent Api 사용

//오늘의 주제 : Foreign key와 nullable
// Dependent data가  Principal data 없이 독립적으로 존재 할 수 있을까 ?
// -1)주인이 없는 아이템은 불가능
// -2)주인이 없는 아이템도 가능 !
// 어떻게 구분해서 설정할까 ?
//- 답은 Nullable ! 


//오늘의 주제 : Entity 상태 관리 (STATE)
//(1) Detached (no tracking : 추적되지 않는 상태 )        -> Add - 
//(2) Added (db에는 아직 없음 , saveChanges 로 db에 적용 ) -> Add + 
// save Changes 호출                                   -> commit 
//  --추가된 객체의 상태가 unChanged으로 바뀜
//  --sql identity으로 PK관리
//-- 데이터 추가후 ID받아 와서 객체의 property를 채워줌
//-- Relationship 참고 하여 , FK 세팅및 객체 참조 연결 
//(3) Deleted(db에 있지만 삭제되어야함 )
//(4) Modified(db에 있고 클라에서 수정 됨 )
//(5) Unchanged(db에 있고 수정사항도 없음 )

//이미 존재하는 사용자를 연결하려면 ?
//(1) Tracked Instance를 가져와서
//(2) 데이터 연동 
//1 +2 ) 모든 길드원들이 가지고 있는 아이템 출력 하기

// update의 기초 : update의 3 단계
//1) Tracked Entity를 얻어 온다
//2) entity의 property를 변경한다 (set)
//3) saveChangesg 호출 (commit)

// (connected vs disconnected ) 업데이트 
// disconnected -> update 1+2+3 이 한번에 쭉 일어나지 않음

//업데이트 처리 방식 2가지
//1) Reload 방식
//2) Full Update 방식 -> 모든 정보를 통으로 받아서 아예 Entity를 다시 만들기 


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

