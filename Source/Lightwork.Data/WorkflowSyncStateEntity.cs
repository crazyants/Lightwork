using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace D3.Lightwork.Data
{
    [Table("WorkflowSyncStates")]
    public class WorkflowSyncStateEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Index("IX_SyncState", 1, IsUnique = true)]
        public Guid WorkflowId { get; set; }

        [Index("IX_SyncState", 2, IsUnique = true)]
        [MaxLength(255)]
        public string SyncId { get; set; }

        public Guid WorkflowParentId { get; set; }

        public string WorkflowType { get; set; }

        public string State { get; set; }

        public DateTime DateCreated { get; set; }
    }
}
