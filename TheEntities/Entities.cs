using System;   
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ienablemuch.DitTO;

namespace TheEntities.Poco
{

    #region Entities

    public class Customer
    {
        public virtual int CustomerId { get; set; }
        public virtual string CustomerName { get; set; }
        public virtual string Address1 { get; set; }
        public virtual int MemberYear { get; set; }
    }

    public class Order
    {
        public virtual int OrderId { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual string OrderDescription { get; set; }
        public virtual DateTime OrderDate { get; set; }

        public virtual IList<OrderLine> OrderLines { get; set; }
    }

    public class OrderLine
    {
        public virtual Order Order { get; set; }

        public virtual int OrderLineId { get; set; }
        public virtual Product Product { get; set; }        
        public virtual int Quantity { get; set; }
        public virtual decimal Price { get; set; }
        public virtual decimal Amount{ get; set; }
        public virtual Product Freebie { get; set; }

        public virtual IList<Comment> Comments { get; set; }
    }


    public class Product
    {
        public virtual int ProductId { get; set; }
        public virtual string ProductName { get; set; }
        public virtual string ProductDescription { get; set; }
    }


    public class Comment
	{
        public virtual OrderLine OrderLine { get; set; }

		public virtual int CommentId { get; set; }        
        public virtual string TheComment { get; set; }
	}

    #endregion




}


namespace TheEntities.Dto
{
    public class OrderDto
    {
        public int OrderId { get; set; }

        [PocoMapping("Customer", "CustomerId", true)]
        public int CustomerID { get; set; }
        [PocoMapping("Customer", "CustomerName")]
        public string CustomerName { get; set; }
        [PocoMapping("Customer", "Address1")]
        public string Address1 { get; set; }
        [PocoMapping("Customer", "MemberYear")]
        public int MemberYear { get; set; }

        public string OrderDescription { get; set; }
        public DateTime OrderDate { get; set; }

        [PocoCollectionLink("Order")]
        public IList<OrderLineDto> OrderLines{ get; set; }
    }

    public class OrderLineDto
    {        
        public int OrderLineId { get; set; }
        
        [PocoMapping("Product", "ProductId", true)]
        public int ProductID { get; set; }
        [PocoMapping("Product", "ProductName")]
        public string ProductName { get; set; }

        [PocoMapping("Freebie", "ProductId", true)]
        public int FreebieID { get; set; }

        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount{ get; set; }

        [PocoCollectionLink("OrderLine")]
        public IList<CommentDto> Comments { get; set; }
    }


    public class CommentDto
	{     
		public int CommentId { get; set; }        
        public string TheComment { get; set; }
	}


}