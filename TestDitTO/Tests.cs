﻿using System;
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

namespace TestDitTO
{
    

    [TestClass]
    public class Tests
    {
        const string connectionString = "Data Source=localhost; Initial Catalog=ObjectGraphDitTO; User id=sa; Password=P@$$w0rd";

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
            Mapper.FromAssemblyOf<OrderMapping>();

            OrderDto odto = Mapper.ToDto<Order, OrderDto>(o);

                        
            // Assert

            Assert.AreEqual(cOrderDescription, odto.OrderDescription);
            Assert.AreEqual(cOrderDate, odto.OrderDate);
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
            Mapper.FromAssemblyOf<OrderMapping>();

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

            ISession s = NhDbMapper.GetSession(connectionString);


            // Act
            Order o = s.Query<Order>().OrderBy(x => x.OrderId).First();



            // Assert
            Mapper.FromAssemblyOf<OrderMapping>();
            // var oDto = Poco.ToDto<OrderDto>(o);
            OrderDto oDto = Mapper.ToDto<Order, OrderDto>(o);

            Assert.AreEqual("Hello", oDto.OrderDescription);
            Assert.AreEqual(new DateTime(1976, 11, 5), oDto.OrderDate);
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

            

            // Act
            Order o = db.Set<Order>()
                .Include("OrderLines")
                    .Include("OrderLines.Product")
                .Include("OrderLines.Comments")
                .OrderBy(x => x.OrderId).First();
            


            // Assert            
            Mapper.FromAssemblyOf<OrderMapping>();

            OrderDto odto = Mapper.ToDto<Order, OrderDto>(o);

            Assert.AreEqual("Hello", odto.OrderDescription);
            Assert.AreEqual(new DateTime(1976, 11, 5), odto.OrderDate);
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
            
            Mapper.FromAssemblyOf<OrderMapping>();

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

            Mapper.FromAssemblyOf<OrderMapping>();

            Order oPoco = Mapper.ToPoco<OrderDto,Order>(oDto);

            var db = new EfDbMapper(connectionString);
            // EfPoco.AssignStub<OrderDto>(oPoco, db);

            EfPoco.AssignStub(oPoco, db);

            var repoOrder = new Ienablemuch.ToTheEfnhX.EntityFramework.EfRepository<Order>(db);
            repoOrder.Merge(oPoco, null);


            
            

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
            Mapper.FromAssemblyOf<OrderMapping>();

            OrderDto oDto = Mapper.ToDto<Order,OrderDto>(oPoco);




            // Assert

            // Assert.AreEqual(cOrderDescription, odto.ZOrderDescription);
            Assert.AreEqual(cOrderDate, oDto.OrderDate);
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
            Mapper.FromAssemblyOf<OrderMapping>();

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



    }
}
