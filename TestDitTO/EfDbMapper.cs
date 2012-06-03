using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using TheEntities.Poco;

namespace TestDitTO
{
    public class EfDbMapper : DbContext
    {
        public EfDbMapper(string connectionString) : base(connectionString)
        {
            this.Configuration.ProxyCreationEnabled = false;
        }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();


            modelBuilder.Entity<Order>().HasKey(x => x.OrderId);

            // modelBuilder.Entity<Question>().Property(x => x.RowVersion).IsRowVersion();
            // modelBuilder.Entity<Order>().HasMany(x => x.OrderLines).WithRequired(x => x.Order).Map(x => x.MapKey("Order_OrderId"));
            // modelBuilder.Entity<OrderLine>().HasMany(x => x.Comments).WithRequired(x => x.OrderLine).Map(x => x.MapKey("OrderLine_OrderLineId"));
            //modelBuilder.Entity<OrderLine>().HasOptional(x => x.Freebie).WithOptionalPrincipal().Map(y => y.MapKey("Freebie_ProductId"));
            //modelBuilder.Entity<OrderLine>().HasRequired(x => x.Product).WithRequiredPrincipal().Map(y => y.MapKey("Product_ProductId"));
            // modelBuilder.Entity<OrderLine>().Property(x => x.Freebie).HasColumnName("Product_FreebieId");

            


        }
        


    }
}
