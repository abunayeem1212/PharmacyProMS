using System;
using System.ComponentModel.DataAnnotations;

namespace PharmacyProMS.Models
{
    public class UserActivityLog
    {
        [Key]
        public int LogId { get; set; }

        [StringLength(450)]
        public string UserId { get; set; }

        [StringLength(100)]
        public string UserName { get; set; }

        [StringLength(100)]
        public string Action { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(100)]
        public string TableAffected { get; set; }

        public DateTime LoggedAt { get; set; } = DateTime.Now;
    }
}