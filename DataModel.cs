using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace EFCore
{

    //오늘의 주제 : 초기값 (Default Value)

    //기본값 설정하는 방법이 여러가지가 있음
    //주의해서 볼 것
    // 1) Entity class 자체의 초기값으로 붙는지
    // 2) db 테이블 차원에서 초기값으로 적용되는지
    // -결과는 같은거 아닐까 ?
    // EF - DB 외에 다른 경로로 DB사용한다면 ,차이가 날 수 있다

    // (1) Auto - Property Initializer
    // public DateTime CreateDate {get;set;} = DEFAULT VALUE ; 
    // -> entity 차원의 초기값 -> save Changes
    // (2) fluent API
    //.HasDefaultValue(DEFAULT VALUE)
    // -> SQL Default 적용
    // (3) SQL Fragment(동적으로 DB에서 실행 )
    // ->.HasDefaultSqlValue("GETDATE") <- SQL 인자으로 넣음
    // (4) Value Generator
    //entity 차원에서 초기값을 생성하는 규칙 정해주기
    //.HasValueGenerator() 

    //오늘의 주제 : UDF(User Defined Function)
    //직접 만든 sql쿼리를 실행 하고 싶음
    //(1) configuration
    // - static 함수를 만들고 EfCore에 등록
    //(2) 데이터 베이스 set up
    //(3) 사용


    //오늘의 주제 : Entity <-> DB Table 연동 하는 방법들
    //Entity Class 하나를 통으로 Read/Write -> 부담 (Select Loading / DTO )

    //(1)Owned Type
    // -일반 클래스를 Entity Class에 추가하는 개념
    //a) 동일한 테이블에 추가 (ownership)
    // .OwnsOne()
    // Relationship이 아닌 Ownership의 개념이기 때문에 .Include() x
    //b) 다른 테이블에 추가
    // .OwnsOne().ToTable("ItemOption")


    //(2)Table Per Hierarchy
    // - 상속 관계의 여러 클래스를 하나의 테이블에 매핑
    //ex) Cat ,Dog,Bird, Animal
    // a) convention 방식
    //- 일단 class를 상속 받아 만들고 , DbSet 추가
    //- Discriminator ?
    // b) fluent Api
    // - .HasDiscriminator().HasValue("Itemtype.NormalItem")
    //(3)Table Split
    // - 다수의 Entity Class <-> 하나의 테이블에 매핑
    //.ToTable("TableName")
    // 테이블을 쪼개서 알뜰하게 사용 !

    //오늘의 주제 : backingField + Relationship
    //Backing Field -> private property를 Db애 매핑
    //Navigational property에서도 사용 가능 ! -> 구체적인 예시로 기억하자 ! 

    public class ItemReview
    {
        public int ItemReviewId { get; set; }
        public int Score { get; set; }
    }

    [Table("Item")]
    public class Item
    {


        public bool SoftDeleted { get; set; }// 데이터 복구를 위해 

        public int ItemId { get; set; }//이름id = PK 
        public int TemplateId { get; set; }
        public DateTime CreateDate { get; set; } //entity 차원 
        
        public int OwnerId { get; set; }
        public Player Owner { get; set; }//다른 클래스 참조 -> FK

        //다른 테이블에서 모든 정보를 불러와야 함 => 부담!
        //외부 테이블이 변경 될때 동시에 내부 테이블 값이 변화하도록 설계

        public List<ItemReview>? Reviews { get; set; }

    }

    [Table("Player")]
    public class Player
    {
        //이름id = PK 
        public int PlayerId { get; set; }
        [Required]
        [MaxLength(20)]
        public string Name { get; set; }

        public Item OwnedItem { get; set; }

    }

    [Table("Guild")]
    public class Guild
    {
        public int GuildId { get; set; }
        public string GuildName { get; set; }

        public ICollection<Player> Members { get; set; } //일대다 모델링 
    }

    //DTO(Data Transfer Object)
    public class GuildDto
    {
        public int GuildId { get; set; }
        public string Name { get; set; }
        public int MemberCount { get; set; }
    }
}

