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
            var odto = Poco.ToDto<OrderDto>(o);

                        
            // Assert

            Assert.AreEqual(cOrderDescription, odto.OrderDescription);
            Assert.AreEqual(cOrderDate, odto.OrderDate);
            Assert.AreEqual(cOrderId, odto.OrderId);

            Assert.IsNotNull(odto.OrderLines);
            Assert.AreEqual(o.OrderLines.Count, odto.OrderLines.Count);

            Assert.AreEqual(o.OrderLines[0].Product.ProductId, odto.OrderLines[0].ProductID);
            Assert.AreEqual(o.OrderLines[0].Product.ProductName, odto.OrderLines[0].ProductName);
            Assert.AreEqual(o.OrderLines[0].Freebie.ProductId, odto.OrderLines[0].FreebieID);
            Assert.AreEqual(o.OrderLines[0].Quantity, odto.OrderLines[0].Quantity);
            Assert.AreEqual(o.OrderLines[0].Amount, odto.OrderLines[0].Amount);


            Assert.IsNotNull(odto.OrderLines[1].Comments);
            Assert.AreEqual(o.OrderLines[1].Comments.Count, odto.OrderLines[1].Comments.Count);
            Assert.AreEqual(o.OrderLines[1].Comments[0].TheComment, odto.OrderLines[1].Comments[0].TheComment); 

            
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
                CustomerID = 1,
                CustomerName = "Hey",
                OrderLines = new[]
                {
                    new OrderLineDto { OrderLineId = 7, ProductID = 1, Quantity = 7, Price = 6, Amount = 42 },
                    new OrderLineDto 
                    { 
                        ProductID = 2, Quantity = 3, Price = 6, Amount = 18, FreebieID = 9,
                        Comments = new[]
                        {
                            new CommentDto { CommentId = 9, TheComment = "Great" },
                            new CommentDto { TheComment = "Nice" }
                        }
                    },
                    new OrderLineDto { ProductID = 1, Quantity = 4, Price = 5, Amount = 20, FreebieID = 9 },
                }
            };


            // Act 
            Order oPoco = Dto.ToPoco<Order>(oDto);


            // Assert
            Assert.AreEqual(oDto.OrderId, oPoco.OrderId);
            Assert.AreEqual(oDto.CustomerID, oPoco.Customer.CustomerId);
            Assert.AreEqual(oDto.OrderDate, oPoco.OrderDate);

            Assert.AreEqual(oDto.OrderDescription, oPoco.OrderDescription);
            Assert.AreEqual(3, oPoco.OrderLines.Count);
            
            Assert.IsNotNull(oPoco.OrderLines[0].Order);
            Assert.ReferenceEquals(oPoco, oPoco.OrderLines[0].Order);
            Assert.AreEqual(2, oPoco.OrderLines[1].Comments.Count);

            Assert.IsNotNull(oPoco.OrderLines[0].Product);
            Assert.AreEqual(oDto.OrderLines[0].ProductID, oPoco.OrderLines[0].Product.ProductId);
            Assert.AreEqual(oDto.OrderLines[0].ProductName, oPoco.OrderLines[0].Product.ProductName);
            Assert.AreEqual(oDto.OrderLines[0].Quantity, oPoco.OrderLines[0].Quantity);
            Assert.AreEqual(oDto.OrderLines[0].Price, oPoco.OrderLines[0].Price);
            Assert.AreEqual(oDto.OrderLines[0].Amount, oPoco.OrderLines[0].Amount);

            Assert.AreEqual(oDto.OrderLines[1].FreebieID, oPoco.OrderLines[1].Freebie.ProductId);

            Assert.AreEqual(oDto.OrderLines[1].Comments[0].CommentId, oPoco.OrderLines[1].Comments[0].CommentId);
            Assert.AreEqual(oDto.OrderLines[1].Comments[0].TheComment, oPoco.OrderLines[1].Comments[0].TheComment);

            Assert.AreEqual(oPoco.OrderLines[1].Comments[1].CommentId, oPoco.OrderLines[1].Comments[1].CommentId);
            Assert.AreEqual(oDto.OrderLines[1].Comments[1].TheComment, oPoco.OrderLines[1].Comments[1].TheComment);
            

        }


        [TestMethod]
        public void Test_Live_Poco_To_Dto()
        {
            // Arrange
            
            // ISession s = NhDbMapper.GetSession(System.Configuration.ConfigurationManager.ConnectionStrings["EfDbMapper"].ConnectionString);

            ISession s = NhDbMapper.GetSession(connectionString);


            // Act
            Order o = s.Query<Order>().OrderBy(x => x.OrderId).First();



            // Assert
            var oDto = Poco.ToDto<OrderDto>(o);

            Assert.AreEqual("Hello", oDto.OrderDescription);
            Assert.AreEqual(new DateTime(1976, 11, 5), oDto.OrderDate);
            Assert.AreEqual(3, oDto.OrderLines.Count);

            Assert.AreEqual(1, oDto.OrderLines[0].ProductID);
            Assert.AreEqual(2, oDto.OrderLines[0].FreebieID);
            Assert.AreEqual("Mouse", oDto.OrderLines[0].ProductName);
            Assert.AreEqual(2, oDto.OrderLines[1].ProductID);
            Assert.AreEqual(0, oDto.OrderLines[1].FreebieID);
            Assert.AreEqual("Keyboard", oDto.OrderLines[1].ProductName);
            Assert.AreEqual(1, oDto.OrderLines[2].ProductID);
            Assert.AreEqual("Mouse", oDto.OrderLines[2].ProductName);

            Assert.AreEqual(2, oDto.OrderLines[1].Comments.Count);
            Assert.AreEqual("Nice", oDto.OrderLines[1].Comments[0].TheComment);
            Assert.AreEqual("Great", oDto.OrderLines[1].Comments[1].TheComment);
            // Assert.AreEqual("Hello", o.OrderDescription);

        }

        [TestMethod]
        public void Test_Live_Poco_To_Dto_via_EF()
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
            var odto = Poco.ToDto<OrderDto>(o);

            Assert.AreEqual("Hello", odto.OrderDescription);
            Assert.AreEqual(new DateTime(1976, 11, 5), odto.OrderDate);
            Assert.AreEqual(3, odto.OrderLines.Count);

            Assert.AreEqual(1, odto.OrderLines[0].ProductID);
            Assert.AreEqual(2, odto.OrderLines[0].FreebieID);
            Assert.AreEqual("Mouse", odto.OrderLines[0].ProductName);
            Assert.AreEqual(2, odto.OrderLines[1].ProductID);
            Assert.AreEqual(0, odto.OrderLines[1].FreebieID);
            Assert.AreEqual("Keyboard", odto.OrderLines[1].ProductName);
            Assert.AreEqual(1, odto.OrderLines[2].ProductID);
            Assert.AreEqual("Mouse", odto.OrderLines[2].ProductName);

            Assert.AreEqual(2, odto.OrderLines[1].Comments.Count);
            Assert.AreEqual("Nice", odto.OrderLines[1].Comments[0].TheComment);
            Assert.AreEqual("Great", odto.OrderLines[1].Comments[1].TheComment);

        }


        [TestMethod]
        public void Test_Dto_To_Live_Nh_Poco()
        {



            // Arrange
            OrderDto oDto = new OrderDto
            {
                OrderDate = new DateTime(2012, 07, 22),
                OrderDescription = "Hey Apple!",
                CustomerID = 1,
                CustomerName = "Hey",
                OrderLines = new[]
                {
                    new OrderLineDto { ProductID = 1, Quantity = 7, Price = 6, Amount = 42 },
                    new OrderLineDto 
                    { 
                        ProductID = 2, Quantity = 3, Price = 6, Amount = 18, FreebieID = 1,
                        Comments = new[]
                        {
                            new CommentDto { TheComment = "Great" },
                            new CommentDto { TheComment = "Nice" }
                        }
                    },
                    new OrderLineDto { ProductID = 1, Quantity = 4, Price = 5, Amount = 20, FreebieID = 2 },
                }
            };

          

            // Act
            var oPoco = Dto.ToPoco<Order>(oDto);

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
                CustomerID = 1,
                CustomerName = "Hey",
                OrderLines = new[]
                {
                    new OrderLineDto { ProductID = 1, Quantity = 7, Price = 6, Amount = 42 },
                    new OrderLineDto 
                    { 
                        ProductID = 2, Quantity = 3, Price = 6, Amount = 18, FreebieID = 1,
                        Comments = new[]
                        {
                            new CommentDto { TheComment = "Great" },
                            new CommentDto { TheComment = "Nice" }
                        }
                    },
                    new OrderLineDto { ProductID = 1, Quantity = 4, Price = 5, Amount = 20, FreebieID = 2 },
                }
            };



            // Act


            
            Order oPoco = Dto.ToPoco<Order>(oDto);

            var db = new EfDbMapper(connectionString);
            EfPoco.AssignStub<OrderDto>(oPoco, db);

            var repoOrder = new Ienablemuch.ToTheEfnhX.EntityFramework.EfRepository<Order>(db);
            repoOrder.Merge(oPoco, null);


            
            

            // Assert
            Assert.AreNotEqual(0, oPoco.OrderId);
        }


    }
}
