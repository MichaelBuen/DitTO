using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TheEntities.Poco;
using TheEntities.Dto;

using Ienablemuch.DitTO;

using NHibernate;
using NHibernate.Linq;
using Ienablemuch.DitTO.EntityFrameworkStubAssigner;

using EF = Ienablemuch.ToTheEfnhX.EntityFramework;

namespace TestDitTO
{
    

    [TestClass]
    public class Tests
    {
        const string connectionString = "Data Source=localhost; Initial Catalog=ObjectGraphDitTO; User id=sa; Password=P@$$w0rd";

        public Tests()
        {
            Mapper.FromAssemblyOf<OrderMapping>();
        }

        [TestMethod]
        public void Test_Poco_To_Dto()
        {
            const string cOrderDescription = "Hello";
            const int cOrderId = 88;
            
            DateTime cOrderDate = new DateTime(1976, 11, 05);


            // Arrange
            var mouse = new Product { ProductId = 1, ProductName = "Mouse" };
            var keyboard = new Product { ProductId = 2, ProductName = "Keyboard" };

            Order o = new Order { OrderId = 88, OrderDescription = cOrderDescription, OrderDate = cOrderDate };
            o.OrderLines = new List<OrderLine>
            {
                new OrderLine { Order = o, Product = mouse, Quantity = 7, Price = 6, Amount = 42, Freebie = keyboard },
                new OrderLine { Order = o, Product = keyboard, Quantity = 2, Price = 3, Amount = 6 },
                new OrderLine { Order = o, Product = mouse, Quantity = 5, Price = 10, Amount = 50 },
            };

            o.OrderLines[1].Comments = new List<Comment>
            {
                new Comment { OrderLine = o.OrderLines[1], TheComment = "Nice" },
                new Comment { OrderLine = o.OrderLines[1], TheComment = "Great" },
            };



            // Act          
            OrderDto odto = Mapper.ToDto<Order, OrderDto>(o);

                        
            // Assert

            Assert.AreEqual(cOrderDescription, odto.OrderDescription);
            Assert.AreEqual(cOrderDate, odto.DummyDate);
            Assert.AreEqual(cOrderId, odto.OrderId);

            Assert.IsNotNull(odto.OrderLines);
            Assert.AreEqual(o.OrderLines.Count, odto.OrderLines.Count);

            Assert.AreEqual(o.OrderLines[0].Product.ProductId, odto.OrderLines[0].ProductoId);
            Assert.AreEqual(o.OrderLines[0].Product.ProductDescription, odto.OrderLines[0].ProductDescription);
            Assert.AreEqual(o.OrderLines[0].Freebie.ProductId, odto.OrderLines[0].FreebieId);
            Assert.AreEqual(o.OrderLines[0].Quantity, odto.OrderLines[0].Quantity);
            Assert.AreEqual(o.OrderLines[0].Amount, odto.OrderLines[0].Amount);


            Assert.IsNotNull(odto.OrderLines[1].Koments);
            Assert.AreEqual(o.OrderLines[1].Comments.Count, odto.OrderLines[1].Koments.Count);
            Assert.AreEqual(o.OrderLines[1].Comments[0].TheComment, odto.OrderLines[1].Koments[0].TheComment); 

            
        }

        [TestMethod]
        public void Test_Dto_To_Poco()
        {
            // Arrange
            OrderDto oDto = new OrderDto
            {
                OrderId = 76,
                OrderDate = new DateTime(2012, 07, 22),                
                OrderDescription = "Hey Apple!",
                CustomerId = 1,
                CustomerName = "Hey",
                OrderLines = new[]
                {
                    new OrderLineDto { OrderLineId = 7, ProductoId = 1, Quantity = 7, Price = 6, Amount = 42 },
                    new OrderLineDto 
                    { 
                        ProductoId = 2, Quantity = 3, Price = 6, Amount = 18, FreebieId = 9,
                        Koments = new[]
                        {
                            new CommentDto { CommentId = 9, TheComment = "Great" },
                            new CommentDto { TheComment = "Nice" }
                        }
                    },
                    new OrderLineDto { ProductoId = 1, Quantity = 4, Price = 5, Amount = 20, FreebieId = 9 },
                }
            };


            // Act             
            Order oPoco = Mapper.ToPoco<OrderDto, Order>(oDto);

            // Assert
            Assert.AreEqual(oDto.OrderId, oPoco.OrderId);
            Assert.AreEqual(oDto.CustomerId, oPoco.Customer.CustomerId);
            Assert.AreEqual(oDto.OrderDate, oPoco.OrderDate);

            Assert.AreEqual(oDto.OrderDescription, oPoco.OrderDescription);
            Assert.AreEqual(3, oPoco.OrderLines.Count);
            
            Assert.IsNotNull(oPoco.OrderLines[0].Order);
            Assert.ReferenceEquals(oPoco, oPoco.OrderLines[0].Order);
            Assert.AreEqual(2, oPoco.OrderLines[1].Comments.Count);

            Assert.IsNotNull(oPoco.OrderLines[0].Product);
            Assert.AreEqual(oDto.OrderLines[0].ProductoId, oPoco.OrderLines[0].Product.ProductId);
            Assert.AreEqual(oDto.OrderLines[0].ProductDescription, oPoco.OrderLines[0].Product.ProductName);
            Assert.AreEqual(oDto.OrderLines[0].Quantity, oPoco.OrderLines[0].Quantity);
            Assert.AreEqual(oDto.OrderLines[0].Price, oPoco.OrderLines[0].Price);
            Assert.AreEqual(oDto.OrderLines[0].Amount, oPoco.OrderLines[0].Amount);

            Assert.AreEqual(oDto.OrderLines[1].FreebieId, oPoco.OrderLines[1].Freebie.ProductId);

            Assert.AreEqual(oDto.OrderLines[1].Koments[0].CommentId, oPoco.OrderLines[1].Comments[0].CommentId);
            Assert.AreEqual(oDto.OrderLines[1].Koments[0].TheComment, oPoco.OrderLines[1].Comments[0].TheComment);

            Assert.AreEqual(oPoco.OrderLines[1].Comments[1].CommentId, oPoco.OrderLines[1].Comments[1].CommentId);
            Assert.AreEqual(oDto.OrderLines[1].Koments[1].TheComment, oPoco.OrderLines[1].Comments[1].TheComment);
            

        }


        [TestMethod]
        public void Test_Live_NH_Poco_To_Dto()
        {
            // Arrange
            
            // ISession s = NhDbMapper.GetSession(System.Configuration.ConfigurationManager.ConnectionStrings["EfDbMapper"].ConnectionString);

            DateTime expectedDate = new DateTime(1976, 11, 5);

            ISession s = NhDbMapper.GetSession(connectionString);


            // Act
            Order o = s.Query<Order>().OrderBy(x => x.OrderId).First();



            // Assert
            OrderDto oDto = Mapper.ToDto<Order, OrderDto>(o);

            Assert.AreEqual("Hello", oDto.OrderDescription);
            Assert.AreEqual(expectedDate, oDto.DummyDate);
            Assert.AreEqual(3, oDto.OrderLines.Count);

            Assert.AreEqual(1, oDto.OrderLines[0].ProductoId);
            Assert.AreEqual(2, oDto.OrderLines[0].FreebieId);
            Assert.AreEqual("Mouse", oDto.OrderLines[0].ProductDescription);
            Assert.AreEqual(2, oDto.OrderLines[1].ProductoId);
            Assert.AreEqual(0, oDto.OrderLines[1].FreebieId);
            Assert.AreEqual("Keyboard", oDto.OrderLines[1].ProductDescription);
            Assert.AreEqual(1, oDto.OrderLines[2].ProductoId);
            Assert.AreEqual("Mouse", oDto.OrderLines[2].ProductDescription);

            Assert.AreEqual(2, oDto.OrderLines[1].Koments.Count);
            Assert.AreEqual("Nice", oDto.OrderLines[1].Koments[0].TheComment);
            Assert.AreEqual("Great", oDto.OrderLines[1].Koments[1].TheComment);
            // Assert.AreEqual("Hello", o.OrderDescription);

        }

        [TestMethod]
        public void Test_Live_EF_Poco_To_Dto()
        {

            // Arrange
            var db = new EfDbMapper(connectionString);

            DateTime expectedDate = new DateTime(1976, 11, 5);

            

            // Act
            Order o = db.Set<Order>()
                .Include("OrderLines")
                    .Include("OrderLines.Product")
                .Include("OrderLines.Comments")
                .OrderBy(x => x.OrderId).First();
            


            // Assert            
            OrderDto odto = Mapper.ToDto<Order, OrderDto>(o);

            Assert.AreEqual("Hello", odto.OrderDescription);
            Assert.AreEqual(expectedDate, odto.DummyDate);
            Assert.AreEqual(3, odto.OrderLines.Count);

            Assert.AreEqual(1, odto.OrderLines[0].ProductoId);
            Assert.AreEqual(2, odto.OrderLines[0].FreebieId);
            Assert.AreEqual("Mouse", odto.OrderLines[0].ProductDescription);
            Assert.AreEqual(2, odto.OrderLines[1].ProductoId);
            Assert.AreEqual(0, odto.OrderLines[1].FreebieId);
            Assert.AreEqual("Keyboard", odto.OrderLines[1].ProductDescription);
            Assert.AreEqual(1, odto.OrderLines[2].ProductoId);
            Assert.AreEqual("Mouse", odto.OrderLines[2].ProductDescription);

            Assert.AreEqual(2, odto.OrderLines[1].Koments.Count);
            Assert.AreEqual("Nice", odto.OrderLines[1].Koments[0].TheComment);
            Assert.AreEqual("Great", odto.OrderLines[1].Koments[1].TheComment);

        }


        [TestMethod]
        public void Test_Dto_To_Live_Nh_Poco()
        {



            // Arrange
            OrderDto oDto = new OrderDto
            {
                OrderDate = new DateTime(2012, 07, 22),
                OrderDescription = "Hey Apple!",
                CustomerId = 1,
                CustomerName = "Hey",
                OrderLines = new[]
                {
                    new OrderLineDto { ProductoId = 1, Quantity = 7, Price = 6, Amount = 42 },
                    new OrderLineDto 
                    { 
                        ProductoId = 2, Quantity = 3, Price = 6, Amount = 18, FreebieId = 1,
                        Koments = new[]
                        {
                            new CommentDto { TheComment = "Great" },
                            new CommentDto { TheComment = "Nice" }
                        }
                    },
                    new OrderLineDto { ProductoId = 1, Quantity = 4, Price = 5, Amount = 20, FreebieId = 2 },
                }
            };

          

            // Act
            Order oPoco = Mapper.ToPoco<OrderDto,Order>(oDto);

            Order o;
            using (ISession s = NhDbMapper.GetSession(connectionString))
            {
                o = s.Merge(oPoco);                     
                s.Merge(oPoco);
                    
                
                
            }


            // Assert
            Assert.AreNotEqual(0, o.OrderId);
        }


        [TestMethod]
        public void Test_Dto_To_Live_Ef_Poco()
        {



            // Arrange
            OrderDto oDto = new OrderDto
            {
                OrderDate = new DateTime(2012, 07, 22),
                OrderDescription = "Hey Apple!",
                CustomerId = 1,
                CustomerName = "Hey",
                OrderLines = new[]
                {
                    new OrderLineDto { ProductoId = 1, Quantity = 7, Price = 6, Amount = 42 },
                    new OrderLineDto 
                    { 
                        ProductoId = 2, Quantity = 3, Price = 6, Amount = 18, FreebieId = 1,
                        Koments = new[]
                        {
                            new CommentDto { TheComment = "Lovely" },
                            new CommentDto { TheComment = "View" }
                        }
                    },
                    new OrderLineDto { ProductoId = 1, Quantity = 4, Price = 5, Amount = 20, FreebieId = 2 },
                }
            };



            // Act
            Order oPoco = Mapper.ToPoco<OrderDto,Order>(oDto);

            var db = new EfDbMapper(connectionString);
            // EfPoco.AssignStub<OrderDto>(oPoco, db);
            
            db.AssignStub(oPoco);


            var repoOrder = new EF.Repository<Order>(db);
            repoOrder.Merge(oPoco);


            
            

            // Assert
            Assert.AreNotEqual(0, oPoco.OrderId);
        }


        [TestMethod]
        public void Test_Strongly_typed_Mapper_for_Poco_to_Dto() 
        {
            const string cOrderDescription = "Hello";
            const int cOrderId = 88;

            DateTime cOrderDate = new DateTime(1976, 11, 05);


            // Arrange
            var mouse = new Product { ProductId = 1, ProductName = "Mouse" };
            var keyboard = new Product { ProductId = 2, ProductName = "Keyboard" };
            
            var country = new Country { CountryId = 1, CountryName = "Philippines" };
            var customer = new Customer { Country = country, CustomerName = "Michael" };

            Order oPoco = new Order { OrderId = 88, Customer = customer, OrderDescription = cOrderDescription, OrderDate = cOrderDate };
            oPoco.OrderLines = new List<OrderLine>
            {
                new OrderLine { Order = oPoco, Product = mouse, Quantity = 7, Price = 6, Amount = 42, Freebie = keyboard },
                new OrderLine { Order = oPoco, Product = keyboard, Quantity = 2, Price = 3, Amount = 6 },
                new OrderLine { Order = oPoco, Product = mouse, Quantity = 5, Price = 10, Amount = 50 },
            };

            oPoco.OrderLines[1].Comments = new List<Comment>
            {
                new Comment { OrderLine = oPoco.OrderLines[1], TheComment = "Nice" },
                new Comment { OrderLine = oPoco.OrderLines[1], TheComment = "Great" },
            };



            // Act          
            
            /*
            var x = new OrderLineMapping();            
            var odto = new OrderMapping().ToDto(o);
            */
            OrderDto oDto = Mapper.ToDto<Order,OrderDto>(oPoco);




            // Assert

            // Assert.AreEqual(cOrderDescription, odto.ZOrderDescription);
            Assert.AreEqual(cOrderDate, oDto.DummyDate);
            Assert.AreEqual(cOrderId, oDto.OrderId);

            // Assert.AreEqual(o.OrderDescription, odto.CustomerName);            
            Assert.AreEqual(oPoco.Customer.CustomerName, oDto.CustomerName);

            Assert.AreEqual(oPoco.Customer.Address1, oDto.Address1);

            Assert.IsNotNull(oDto.OrderLines);

            Assert.AreNotSame(oPoco.OrderLines, oDto.OrderLines);
            Assert.AreEqual(oPoco.OrderLines.Count, oDto.OrderLines.Count);

            Assert.AreEqual(oPoco.OrderLines[0].Product.ProductId, oDto.OrderLines[0].ProductoId);
            Assert.AreEqual(oPoco.OrderLines[0].Product.ProductDescription, oDto.OrderLines[0].ProductDescription);
            Assert.AreEqual(oPoco.OrderLines[0].Freebie.ProductId, oDto.OrderLines[0].FreebieId);
            Assert.AreEqual(oPoco.OrderLines[0].Quantity, oDto.OrderLines[0].Quantity);
            Assert.AreEqual(oPoco.OrderLines[0].Amount, oDto.OrderLines[0].Amount);


            Assert.AreNotSame(oPoco.OrderLines[1].Comments, oDto.OrderLines[1].Koments);
            Assert.IsNotNull(oDto.OrderLines[1].Koments);
            Assert.AreEqual(oPoco.OrderLines[1].Comments.Count, oDto.OrderLines[1].Koments.Count);
            Assert.AreEqual(oPoco.OrderLines[1].Comments[0].TheComment, oDto.OrderLines[1].Koments[0].TheComment); 

            

        }


        [TestMethod]
        public void Test_Strongly_typed_mapper_for_Dto_To_Poco()
        {
            // Arrange
            OrderDto oDto = new OrderDto
            {
                OrderId = 76,
                OrderDate = new DateTime(2012, 07, 22),
                OrderDescription = "Hey Apple!",
                CustomerId = 5201314,
                CustomerName = "Hey",
                OrderLines = new[]
                {
                    new OrderLineDto { OrderLineId = 7, ProductoId = 1, Quantity = 7, Price = 6, Amount = 42 },
                    new OrderLineDto 
                    { 
                        ProductoId = 2, Quantity = 3, Price = 6, Amount = 18, FreebieId = 9,
                        Koments = new[]
                        {
                            new CommentDto { CommentId = 9, TheComment = "Great" },
                            new CommentDto { TheComment = "Nice" }
                        }
                    },
                    new OrderLineDto { ProductoId = 1, Quantity = 4, Price = 5, Amount = 20, FreebieId = 9 },
                }
            };


            // Act 
            Order oPoco = Mapper.ToPoco<OrderDto, Order>(oDto);


            // Assert
            Assert.AreEqual(oDto.OrderId, oPoco.OrderId);
            Assert.AreEqual(oDto.CustomerId, oPoco.Customer.CustomerId);
            Assert.AreEqual(oDto.OrderDate, oPoco.OrderDate);

            Assert.AreEqual(oDto.OrderDescription, oPoco.OrderDescription);
            Assert.AreEqual(3, oPoco.OrderLines.Count);

            Assert.IsNotNull(oPoco.OrderLines[0].Order);
            Assert.ReferenceEquals(oPoco, oPoco.OrderLines[0].Order);
            Assert.AreEqual(2, oPoco.OrderLines[1].Comments.Count);

            Assert.IsNotNull(oPoco.OrderLines[0].Product);
            Assert.AreEqual(oDto.OrderLines[0].ProductoId, oPoco.OrderLines[0].Product.ProductId);
            Assert.AreEqual(oDto.OrderLines[0].ProductDescription, oPoco.OrderLines[0].Product.ProductName);
            Assert.AreEqual(oDto.OrderLines[0].Quantity, oPoco.OrderLines[0].Quantity);
            Assert.AreEqual(oDto.OrderLines[0].Price, oPoco.OrderLines[0].Price);
            Assert.AreEqual(oDto.OrderLines[0].Amount, oPoco.OrderLines[0].Amount);

            Assert.AreEqual(oDto.OrderLines[1].FreebieId, oPoco.OrderLines[1].Freebie.ProductId);

            Assert.AreEqual(oDto.OrderLines[1].Koments[0].CommentId, oPoco.OrderLines[1].Comments[0].CommentId);
            Assert.AreEqual(oDto.OrderLines[1].Koments[0].TheComment, oPoco.OrderLines[1].Comments[0].TheComment);

            Assert.AreEqual(oPoco.OrderLines[1].Comments[1].CommentId, oPoco.OrderLines[1].Comments[1].CommentId);
            Assert.AreEqual(oDto.OrderLines[1].Koments[1].TheComment, oPoco.OrderLines[1].Comments[1].TheComment);


        }



        [TestMethod]
        public void Test_Live_NH_Language_Country()
        {
            // Arrange
            ISession s = NhDbMapper.GetSession(connectionString);


            // Act
            int c = s.Query<Order>().Where(x => x.OrderId == 1)
                    .SelectMany(x => x.Customer.Country.Languages).Count();
            var langs = s.Query<Order>().Where(x => x.OrderId == 1).SelectMany(x => x.Customer.Country.Languages).OrderBy(x => x.LanguageName);


            // Assert
            Assert.AreEqual(2, c);
            Assert.AreEqual("English", langs.ToArray()[0].LanguageName);
            Assert.AreEqual("Tagalog", langs.ToArray()[1].LanguageName);
        }

        [TestMethod]
        public void Test_Live_Ef_Language_Country()
        {
            

            // Arrange            
            var db = new EfDbMapper(connectionString);

            // Act
            int c = db.Set<Order>().Where(x => x.OrderId == 1)
                    .SelectMany(x => x.Customer.Country.Languages).Count();

            var langs = db.Set<Order>().Where(x => x.OrderId == 1).SelectMany(x => x.Customer.Country.Languages).OrderBy(x => x.LanguageName);

            // Assert
            Assert.AreEqual(2, c);
            Assert.AreEqual("English", langs.ToArray()[0].LanguageName);
            Assert.AreEqual("Tagalog", langs.ToArray()[1].LanguageName);

        }


        [TestMethod]
        public void Test_Live_Ef_Language_Country_Poco_to_Dto()
        {
            // Arrange            
            var db = new EfDbMapper(connectionString);
                
            // Act
            int c = db.Set<Order>().Where(x => x.OrderId == 1)
                    .SelectMany(x => x.Customer.Country.Languages).Count();

            var langs = db.Set<Order>().Where(x => x.OrderId == 1).SelectMany(x => x.Customer.Country.Languages).OrderBy(x => x.LanguageName);

            Order oPoco = db.Set<Order>().Single(x => x.OrderId == 1);
            OrderDto oDto = Mapper.ToDto<Order,OrderDto>(oPoco);
            Assert.AreEqual(2, oPoco.Customer.Country.Languages.Count());
            Assert.IsNotNull(oDto);

            // go back
            // Assert.IsNotNull(oDto.PossibleLanguages);
            // var dtoLang = oDto.PossibleLanguages.OrderBy(x => x.LanguageName).ToArray();
            


            // Assert
            Assert.AreEqual(2, c);
            Assert.AreEqual("English", langs.ToArray()[0].LanguageName);
            Assert.AreEqual("Tagalog", langs.ToArray()[1].LanguageName);


            // go back
            /*
            Assert.AreNotSame(oPoco.Customer.Country.Languages, oDto.PossibleLanguages);
            Assert.AreEqual("English", dtoLang[0].LanguageName);
            Assert.AreEqual("Tagalog", dtoLang[1].LanguageName);
            */

        }


        [TestMethod]
        public void Test_Live_Nh_Language_Country_Poco_to_Dto()
        {
            // Arrange            
            var db = NhDbMapper.GetSession(connectionString);

            // Act
            int c = db.Query<Order>().Where(x => x.OrderId == 1)
                    .SelectMany(x => x.Customer.Country.Languages).Count();

            var langs = db.Query<Order>().Where(x => x.OrderId == 1).SelectMany(x => x.Customer.Country.Languages).OrderBy(x => x.LanguageName);

            Order oPoco = db.Query<Order>().Single(x => x.OrderId == 1);
            OrderDto oDto = Mapper.ToDto<Order, OrderDto>(oPoco);
            Assert.AreEqual(2, oPoco.Customer.Country.Languages.Count());
            Assert.IsNotNull(oDto);

            // go back
            
            // Assert.IsNotNull(oDto.PossibleLanguages);
            // var dtoLang = oDto.PossibleLanguages.OrderBy(x => x.LanguageName).ToArray();



            // Assert
            Assert.AreEqual(2, c);
            Assert.AreEqual("English", langs.ToArray()[0].LanguageName);
            Assert.AreEqual("Tagalog", langs.ToArray()[1].LanguageName);

            // go back
            // Assert.AreNotSame(oPoco.Customer.Country.Languages, oDto.PossibleLanguages);
            // Assert.AreEqual("English", dtoLang[0].LanguageName);
            // Assert.AreEqual("Tagalog", dtoLang[1].LanguageName);

            
            


        }

        [TestMethod]
        public void Test_nested()
        {

            // Arrange

            int customerId = 1;

            OrderDto oDto = new OrderDto
            {
                CustomerId = customerId,
                CustomerName = "Michael",
                OrderDescription = "Superb"
            };

            // Act
            Order oPoco = Mapper.ToPoco<OrderDto, Order>(oDto);

            // Assert            
            Assert.AreNotSame(oDto, oPoco);
            Assert.IsNotNull(oPoco.Customer);            
            Assert.AreEqual(customerId,oPoco.Customer.CustomerId);

            // Even we have a Customer object. it's just a stub object. Expect other properties to be null or zero, i.e. in their default value
            Assert.IsNull(oPoco.Customer.CustomerName);

            Assert.IsNull(oPoco.Customer.Country);
        }


        [TestMethod]
        public void Test_nested_Live_Nh_Dto_to_Poco()
        {            
            

            int customerId = 1;

            // Arrange
            OrderDto oDto = new OrderDto
            {
                CustomerId = customerId,
                CustomerName = "Miguel",
                OrderDescription = "Superb",
                OrderDate = new DateTime(2076,11,05),
                OrderLines = new[] 
                { 
                    new OrderLineDto { ProductoId = 1, Quantity = 8, Price = 6, Amount = 48 },
                    new OrderLineDto { ProductoId = 2, Quantity = 3, Price = 6, Amount = 18 }
                }
            };

            // Act
            Order oPoco = Mapper.ToPoco<OrderDto, Order>(oDto);

            
            // Assert            
            Assert.AreNotSame(oDto, oPoco);
            Assert.IsNotNull(oPoco.Customer);
            Assert.AreEqual(customerId, oPoco.Customer.CustomerId);
            

            // Even we have a Customer object. it's just a stub object. Expect other properties to be null or zero, i.e. in their default value
            Assert.IsNull(oPoco.Customer.CustomerName);

            // And so is this
            Assert.IsNull(oPoco.Customer.Country);


            ISession s = NhDbMapper.GetSession(connectionString);
            
            oPoco = s.Merge(oPoco);
            s.Flush();


            Assert.AreEqual(2, oPoco.OrderLines.Count);
            Assert.AreNotEqual(0, oPoco.OrderId);
            Assert.AreEqual(oDto.OrderDescription, oPoco.OrderDescription);
            // the customer name from DTO would not cascade to POCO. referential integrity is maintained                
            Assert.AreEqual("Michael", oPoco.Customer.CustomerName);                
            Assert.AreEqual("Philippines", oPoco.Customer.Country.CountryName);
                
            
        }


        [TestMethod]
        public void Test_nested_Live_Ef_Dto_to_Poco()
        {
            // Arrange
            int customerId = 1;
            string orderDesc = "Superb";

            OrderDto oDto = new OrderDto
            {
                CustomerId = customerId,
                CustomerName = "Miguel",
                OrderDescription = orderDesc,
                OrderDate = new DateTime(2076, 11, 05),
                OrderLines = new[] 
                { 
                    new OrderLineDto { ProductoId = 1, Quantity = 8, Price = 6, Amount = 48 },
                    new OrderLineDto { ProductoId = 2, Quantity = 3, Price = 6, Amount = 18 }
                }

            };

            // Act
            Order oPoco = Mapper.ToPoco<OrderDto, Order>(oDto);

            // Assert            
            Assert.AreNotSame(oDto, oPoco);
            Assert.IsNotNull(oPoco.Customer);
            Assert.AreEqual(customerId, oPoco.Customer.CustomerId);
            

            // Even we have a Customer object. it's just a stub object. Expect other properties to be null or zero, i.e. in their default value
            Assert.IsNull(oPoco.Customer.CustomerName);

            // And so is this
            Assert.IsNull(oPoco.Customer.Country);


            EfDbMapper db = new EfDbMapper(connectionString);
                        
            db.AssignStub(oPoco);

            
            var repo = new EF.Repository<Order>(db);
            repo.Merge(oPoco);

            /*db.Set<Order>().Add(oPoco);
            db.SaveChanges();*/
            

            Assert.AreEqual(2, oPoco.OrderLines.Count);

            int retId = oPoco.OrderId;

            oPoco = db.Set<Order>().AsNoTracking().Single(x => x.OrderId == retId);

            Customer cl = db.Set<Customer>().AsNoTracking().Single(x => x.CustomerId == 2);
            Assert.AreEqual("Lennon", cl.CustomerName);
            
            Customer c = db.Set<Customer>().AsNoTracking().Single(x => x.CustomerId == 1);
            Assert.AreEqual("Michael", c.CustomerName);

            Assert.AreNotEqual(0, oPoco.OrderId);
            Assert.AreEqual(oDto.OrderDescription, oPoco.OrderDescription);
            // the customer name from DTO would not cascade to POCO. referential integrity is maintained                
            Assert.AreEqual("Michael", oPoco.Customer.CustomerName);                
            Assert.AreEqual("Philippines", oPoco.Customer.Country.CountryName);
            Assert.AreEqual(1940, oPoco.Customer.MemberYear);
            Assert.IsNotNull(oPoco.Customer.Address1);
            

            

        }//Test_nested_Live_Ef_Dto_to_Poco()





        [TestMethod]
        public void Test_Live_Ef_Language_Country_Poco_to_Dto_NullableCountry()
        {
            // Arrange            
            var db = new EfDbMapper(connectionString);
            int orderId = 2;


            // Act
            int c = db.Set<Order>().Where(x => x.OrderId == orderId)
                    .SelectMany(x => x.Customer.Country.Languages).Count();

            var langs = db.Set<Order>().Where(x => x.OrderId == orderId).SelectMany(x => x.Customer.Country.Languages).OrderBy(x => x.LanguageName);


                      
            Order oPoco = db.Set<Order>().Single(x => x.OrderId == orderId);
            OrderDto oDto = Mapper.ToDto<Order, OrderDto>(oPoco);
            

            // Assert
            Assert.AreEqual(0, c);
            Assert.IsNotNull(oPoco.Customer);
            Assert.IsNull(oPoco.Customer.Country);
           
            // go back
            // Assert.IsNull(oDto.PossibleLanguages);
            

        }

        [TestMethod]
        public void Test_Corner_Cases_on_Live_Ef_to_Poco()
        {
            var db = new EfDbMapper(connectionString);
            var repo = new EF.Repository<Order>(db);

            var customerStub = new EF.Repository<Customer>(db);
            Customer cx = customerStub.LoadStub(1);




            Order oPoco = repo.Get(1);


            {
                System.Reflection.PropertyInfo px = oPoco.GetType().GetProperty("Customer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                oPoco.GetType().InvokeMember("Customer", System.Reflection.BindingFlags.GetProperty, null, oPoco, new object[] { });


                var a = oPoco.OrderLines;
                var b = a[0].Order;



                object x = oPoco.Customer;

                var languages = oPoco.Customer.Country.Languages;

                // To avoid circular reference
                // http://connect.microsoft.com/VisualStudio/feedback/details/679399/entity-framework-4-1-using-lazyloading-with-notracking-option-causes-invalidoperationexception
                // object countries = languages[0].Countries;

                // Do these instead:
                int lid = languages.First().LanguageId;
                Language firstLanguage = new EF.Repository<Language>(db).All.SingleOrDefault(l => l.LanguageId == lid);
                object countries = firstLanguage.Countries;
                
            }

            Assert.AreEqual("Michael", oPoco.Customer.CustomerName);
            Assert.AreEqual("Philippines", oPoco.Customer.Country.CountryName);

            Assert.AreEqual(1, oPoco.OrderLines[0].Product.ProductId);



            OrderDto oDto = Mapper.ToDto<Order, OrderDto>(oPoco);


            Assert.AreEqual("Michael", oDto.CustomerName);


            Assert.AreEqual(3, oDto.OrderLines.Count);


        }

        [TestMethod]
        public void Test_Mapping_Corner_Cases()
        {
            // Arrange
            var dx = new DateTime(1976, 11, 05); 
            Order poco = new Order { OrderDate = dx };

            // Act
            OrderDto dto = Mapper.ToDto<Order, OrderDto>(poco);

            // Assert
            Assert.AreEqual(1976, dto.MemberYear);
            Assert.AreNotEqual(dx, dto.OrderDate);
            Assert.AreEqual(dx, dto.DummyDate);
        }


    }
}
