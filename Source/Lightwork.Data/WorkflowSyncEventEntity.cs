using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace D3.Lightwork.Data
{
    [Table("WorkflowSyncEvents")]
    public class WorkflowSyncEventEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Index("IX_SyncEvent", 1, IsUnique = true)]
        public Guid WorkflowId { get; set; }

        [Index("IX_SyncEvent", 2, IsUnique = true)]
        public StoreEvents StoreEvent { get; set; }

        public Guid WorkflowParentId { get; set; }

        public string WorkflowType { get; set; }

        public string State { get; set; }

        public DateTime DateCreated { get; set; }
    }
}
