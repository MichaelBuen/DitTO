using System;
using System.Collections.Generic;
using System.Linq;

using NHibernate;

using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Automapping;
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Instances;

using TheEntities.Poco;


namespace TestDitTO
{
    internal static class NhDbMapper
    {
        public static ISession GetSession(string connectionString)
        {
            return GetSessionFactory(connectionString).OpenSession();
        }


        static ISessionFactory _sf = null;
        private static ISessionFactory GetSessionFactory(string connectionString)
        {
            if (_sf != null) return _sf;





            var fc = Fluently.Configure()
                    .Database(MsSqlConfiguration.MsSql2008.ConnectionString(connectionString))
                    .Mappings
                    (m =>


                            m.AutoMappings.Add
                            (
                                AutoMap.AssemblyOf<Order>(new CustomConfiguration())

                               .Conventions.Add<HasManyConvention>()
                               .Conventions.Add<RowversionConvention>()
 
                               .Override<Country>(x => x.HasManyToMany(y => y.Languages).Table("LanguageAssocCountry").ParentKeyColumn("AssocCountryId").ChildKeyColumn("AssocLanguageId"))
                               .Override<Language>(x => x.HasManyToMany(y => y.Countries).Table("LanguageAssocCountry").ParentKeyColumn("AssocLanguageId").ChildKeyColumn("AssocCountryId"))

                               .Override<Customer>(x =>
                                   {
                                       x.References(z => z.Country).Column("Country_CountryId");
                                       x.Id(z => z.CustomerId).GeneratedBy.Identity();
                                   })
                                .Override<Order>(x =>
                                    {
                                        x.Id(z => z.OrderId).GeneratedBy.Identity();
                                        
                                        x.References(z => z.Customer).Column("Customer_CustomerId");
                                        // x.References(z => z.Customer);

                                        x.HasMany(z => z.OrderLines).KeyColumn("Order_OrderId");
                                    })
                                .Override<OrderLine>(x =>
                                    {
                                        

                                        x.References(z => z.Order).Column("Order_OrderId");

                                        
                                        x.References(z => z.Product).Column("Product_ProductId");
                                        x.References(z => z.Freebie).Column("Freebie_ProductId");

                                        /*x.References(z => z.Product);
                                        x.References(z => z.Freebie);*/

                                        x.HasMany(z => z.Comments).KeyColumn("OrderLine_OrderLineId");                                        
                                    })
                                .Override<Comment>(x =>
                                    {
                                        x.References(z => z.OrderLine).Column("OrderLine_OrderLineId");                                        
                                        // x.References(z => z.OrderLine);
                                    })

                            )


           );



            _sf = fc.BuildSessionFactory();
            return _sf;
        }


        class CustomConfiguration : DefaultAutomappingConfiguration
        {
            IList<Type> _objectsToMap = new List<Type>()
            {
                // whitelisted objects to map
                typeof(Order), typeof(OrderLine), typeof(Product), typeof(Comment), typeof(Customer), typeof(Country), typeof(Language)
            };
            public override bool ShouldMap(Type type) { return _objectsToMap.Any(x => x == type); }
            public override bool IsId(FluentNHibernate.Member member) { return member.Name == member.DeclaringType.Name + "Id"; }

            public override bool IsVersion(FluentNHibernate.Member member) { return member.Name == "RowVersion"; }
        }



        class HasManyConvention : IHasManyConvention
        {

            public void Apply(IOneToManyCollectionInstance instance)
            {                
                instance.Inverse();
                instance.Cascade.AllDeleteOrphan();
            }


        }

        class RowversionConvention : IVersionConvention
        {
            public void Apply(IVersionInstance instance) { instance.Generated.Always(); }
        }

    }//ModelsMapper

}
