using System;
using System.ComponentModel.DataAnnotations;

namespace PharmacyProMS.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }

        [StringLength(150)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Message { get; set; }

        [StringLength(50)]
        public string Type { get; set; } // Expiry, LowStock, OutOfStock

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? MedicineId { get; set; }
        public virtual Medicine Medicine { get; set; }
    }
}