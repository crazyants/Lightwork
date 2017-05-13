using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace D3.Lightwork.Tests
{
    [Table("TestTable")]
    public class TestTable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string StringColumn1 { get; set; }

        public string StringColumn2 { get; set; }

        public int IntColumn1 { get; set; }

        public int IntColumn2 { get; set; }
    }

    public class TestDbContext : DbContext
    {
        public IDbSet<TestTable> TestTable { get; set; } 
    }
}
