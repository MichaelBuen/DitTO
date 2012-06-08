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
            // this.Configuration.ProxyCreationEnabled = false;


            // Set ProxyCreationEnabled to true, so we can reach the deep part of an object, e.g. orderPoco.Customer.Country.Languages.
            // If we will use false, we need to eager load the Customer.Country.Languages,
            // i.e. orderPoco.Include("Customer").Include("Customer.Country").Include("Customer.Country.Languages")
            
            // true is the default
            // this.Configuration.ProxyCreationEnabled = true;
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


            modelBuilder.Entity<Country>().HasMany(x => x.Languages).WithMany(x => x.Countries)
                .Map(m =>
                    {
                        m.ToTable("LanguageAssocCountry");
                        m.MapLeftKey("AssocCountryId");
                        m.MapRightKey("AssocLanguageId");
                    });


            //modelBuilder.Entity<Language>().HasMany(x => x.Countries).WithMany(x => x.Languages)
            //    .Map(m =>
            //    {
            //        m.ToTable("LanguageAssocCountry");                    
            //        m.MapLeftKey("AssocLanguageId");
            //        m.MapRightKey("AssocCountryId");
            //    });

    
        
            

        }
        


    }
}
