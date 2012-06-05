using System;   
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ienablemuch.DitTO;
using TheEntities.Poco;
using System.Linq.Expressions;

namespace TheEntities.Poco
{

    public class Country
    {
        public virtual int CountryId { get; set; }
        public virtual string CountryName { get; set; }       
    }


    public class Customer
    {
        public virtual int CustomerId { get; set; }
        public virtual string CustomerName { get; set; }
        public virtual Country Country { get; set; }
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





}


namespace TheEntities.Dto
{
    public class OrderDto
    {
        public int OrderId { get; set; }

        
        public int CustomerId { get; set; }

        
        public string CustomerName { get; set; }

        
        
        public string Address1 { get; set; }

        
        public int MemberYear { get; set; }

        public string CountryName { get; set; }

        public string OrderDescription { get; set; }
        public DateTime OrderDate { get; set; }

        
        public IList<OrderLineDto> OrderLines{ get; set; }
    }


    public class OrderLineDto
    {        
        public int OrderLineId { get; set; }
        
        
        public int ProductoId { get; set; }
        
        public string ProductDescription { get; set; }

        
        public int FreebieId { get; set; }

        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount{ get; set; }

        
        public IList<CommentDto> Koments { get; set; }
    }


    public class CommentDto
	{     
		public int CommentId { get; set; }        
        public string TheComment { get; set; }
	}


}