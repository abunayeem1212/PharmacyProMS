using PharmacyProMS.Data;
using PharmacyProMS.Models;
using System;
using System.Linq;

namespace PharmacyProMS.Helpers
{
    public static class NotificationHelper
    {
        public static void GenerateNotifications()
        {
            try
            {
                var db = new ApplicationDbContext();
                var today = DateTime.Today;
                var day30 = today.AddDays(30);
                var setting = db.PharmacySettings
                    .FirstOrDefault();
                int threshold = setting != null
                    ? setting.LowStockThreshold : 10;

                // Clear old unread notifications
                var old = db.Notifications
                    .Where(n => !n.IsRead &&
                        n.CreatedAt < today.AddDays(-1))
                    .ToList();
                db.Notifications.RemoveRange(old);

                // Expiry Alert
                var expiring = db.MedicineBatches
                    .Include("Medicine")
                    .Where(b => b.ExpiryDate <= day30 &&
                        b.ExpiryDate >= today &&
                        b.Quantity > 0)
                    .ToList();

                foreach (var b in expiring)
                {
                    bool exists = db.Notifications.Any(n =>
                        n.MedicineId == b.MedicineId &&
                        n.Type == "Expiry" &&
                        !n.IsRead);
                    if (!exists)
                    {
                        int days = (b.ExpiryDate - today).Days;
                        db.Notifications.Add(new Notification
                        {
                            Title = "Expiry Alert",
                            Message = (b.Medicine != null
                                ? b.Medicine.MedicineName
                                : "Medicine")
                                + " expires in " + days
                                + " days (Batch: "
                                + b.BatchNumber + ")",
                            Type = "Expiry",
                            IsRead = false,
                            CreatedAt = DateTime.Now,
                            MedicineId = b.MedicineId
                        });
                    }
                }

                // Low Stock Alert
                var lowStock = db.Medicines
                    .Where(m => m.IsActive &&
                        m.CurrentStock <= threshold &&
                        m.CurrentStock > 0)
                    .ToList();

                foreach (var m in lowStock)
                {
                    bool exists = db.Notifications.Any(n =>
                        n.MedicineId == m.MedicineId &&
                        n.Type == "LowStock" &&
                        !n.IsRead);
                    if (!exists)
                    {
                        db.Notifications.Add(new Notification
                        {
                            Title = "Low Stock",
                            Message = m.MedicineName
                                + " is running low. Stock: "
                                + m.CurrentStock,
                            Type = "LowStock",
                            IsRead = false,
                            CreatedAt = DateTime.Now,
                            MedicineId = m.MedicineId
                        });
                    }
                }

                // Out of Stock Alert
                var outOfStock = db.Medicines
                    .Where(m => m.IsActive &&
                        m.CurrentStock == 0)
                    .ToList();

                foreach (var m in outOfStock)
                {
                    bool exists = db.Notifications.Any(n =>
                        n.MedicineId == m.MedicineId &&
                        n.Type == "OutOfStock" &&
                        !n.IsRead);
                    if (!exists)
                    {
                        db.Notifications.Add(new Notification
                        {
                            Title = "Out of Stock",
                            Message = m.MedicineName
                                + " is OUT OF STOCK!",
                            Type = "OutOfStock",
                            IsRead = false,
                            CreatedAt = DateTime.Now,
                            MedicineId = m.MedicineId
                        });
                    }
                }

                db.SaveChanges();
                db.Dispose();
            }
            catch { }
        }
    }
}