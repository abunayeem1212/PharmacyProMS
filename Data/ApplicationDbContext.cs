using Microsoft.AspNet.Identity.EntityFramework;
using PharmacyProMS.Migrations;
using PharmacyProMS.Models;
using System.Data.Entity;
using static System.Data.Entity.Infrastructure.Design.Executor;
using static System.Data.Entity.Migrations.Model.UpdateDatabaseOperation;

namespace PharmacyProMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("PharmacyProMS")  // ← Connection String নাম
        {
            this.Configuration.LazyLoadingEnabled = false;
            this.Configuration.ProxyCreationEnabled = false;
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        // All Tables
        public DbSet<Company> Companies { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<MedicineCategory> MedicineCategories { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<MedicineBatch> MedicineBatches { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
        public DbSet<SaleInvoice> SaleInvoices { get; set; }
        public DbSet<SaleInvoiceItem> SaleInvoiceItems { get; set; }
        public DbSet<PharmacySetting> PharmacySettings { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }

        // এই ৪টা line add করো
        public DbSet<SaleReturn> SaleReturns { get; set; }
        public DbSet<SaleReturnItem> SaleReturnItems { get; set; }
        public DbSet<PurchaseReturn> PurchaseReturns { get; set; }
        public DbSet<PurchaseReturnItem> PurchaseReturnItems { get; set; }

        public DbSet<CustomerPayment> CustomerPayments
        { get; set; }

        public DbSet<SupplierPayment> SupplierPayments
        { get; set; }


        public DbSet<StockAdjustment> StockAdjustments
        { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cascade delete বন্ধ করো
            modelBuilder.Entity<PurchaseInvoiceItem>()
                .HasRequired(p => p.PurchaseInvoice)
                .WithMany(p => p.PurchaseItems)
                .HasForeignKey(p => p.PurchaseId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PurchaseInvoiceItem>()
                .HasRequired(p => p.Medicine)
                .WithMany(m => m.PurchaseItems)
                .HasForeignKey(p => p.MedicineId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SaleInvoiceItem>()
                .HasRequired(s => s.SaleInvoice)
                .WithMany(s => s.SaleItems)
                .HasForeignKey(s => s.InvoiceId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SaleInvoiceItem>()
                .HasRequired(s => s.Medicine)
                .WithMany(m => m.SaleItems)
                .HasForeignKey(s => s.MedicineId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Medicine>()
                .HasRequired(m => m.Company)
                .WithMany(c => c.Medicines)
                .HasForeignKey(m => m.CompanyId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Medicine>()
                .HasRequired(m => m.Category)
                .WithMany(c => c.Medicines)
                .HasForeignKey(m => m.CategoryId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Supplier>()
                .HasRequired(s => s.Company)
                .WithMany(c => c.Suppliers)
                .HasForeignKey(s => s.CompanyId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<MedicineBatch>()
                .HasRequired(b => b.Medicine)
                .WithMany(m => m.Batches)
                .HasForeignKey(b => b.MedicineId)
                .WillCascadeOnDelete(false);

            // এই lines add করো OnModelCreating এ
            modelBuilder.Entity<SaleReturnItem>()
                .HasRequired(s => s.SaleReturn)
                .WithMany(s => s.ReturnItems)
                .HasForeignKey(s => s.ReturnId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SaleReturnItem>()
                .HasRequired(s => s.Medicine)
                .WithMany()
                .HasForeignKey(s => s.MedicineId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PurchaseReturnItem>()
                .HasRequired(p => p.PurchaseReturn)
                .WithMany(p => p.ReturnItems)
                .HasForeignKey(p => p.PReturnId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PurchaseReturnItem>()
                .HasRequired(p => p.Medicine)
                .WithMany()
                .HasForeignKey(p => p.MedicineId)
                .WillCascadeOnDelete(false);

             modelBuilder.Entity<CustomerPayment>()
                .HasRequired(cp => cp.Customer)
                .WithMany()
                .HasForeignKey(cp => cp.CustomerId)
                .WillCascadeOnDelete(false);



            modelBuilder.Entity<CustomerPayment>()
                .HasOptional(cp => cp.SaleInvoice)
                .WithMany()
                .HasForeignKey(cp => cp.InvoiceId)
                .WillCascadeOnDelete(false);




            modelBuilder.Entity<SupplierPayment>()
                .HasRequired(sp => sp.Supplier)
                .WithMany()
                .HasForeignKey(sp => sp.SupplierId)
                .WillCascadeOnDelete(false);

                modelBuilder.Entity<StockAdjustment>()
                .HasRequired(s => s.Medicine)
                .WithMany()
                .HasForeignKey(s => s.MedicineId)
                .WillCascadeOnDelete(false);

        }

    }
}