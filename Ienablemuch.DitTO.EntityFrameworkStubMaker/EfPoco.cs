using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Reflection;
using System.Data.Objects;
using System.Data;
using System.Collections;

namespace Ienablemuch.DitTO.EntityFrameworkStubAssigner
{
    public static class EfPoco
    {
        public static void AssignStub(this DbContext db, object poco) 
        {
            AssignStubx(poco, db);
        }

        internal static void AssignStubx(object poco, DbContext db)
        {
            IEnumerable<PropertyMapping> piNeedStub = Mapper.StubsNeeded(poco.GetType());
            

            foreach (PropertyMapping pm in piNeedStub)
            {
                // pm.PropertyPoco.First().   e.g. Customer of Customer.CustomerId.    Customer is PropertyPoco[0], CustomerId is PropertyPoco[1]
                PropertyInfo pocoForeign = poco.GetType().GetProperty(pm.PropertyPoco[0], BindingFlags.Public | BindingFlags.Instance);
                if (pocoForeign == null) continue;


                object val = pocoForeign.GetValue(poco, null);
                
                if (val != null)
                {
                    PropertyInfo pocoForeignId = pocoForeign.PropertyType.GetProperty(pm.PropertyPoco.Last(), BindingFlags.Public | BindingFlags.Instance);

                    object id = pocoForeignId.GetValue(val, null);

                    pocoForeign.SetValue(poco, LoadStub(pocoForeign.PropertyType, pm.PropertyPoco.Last(), id, db), null);
                }
                else
                {
                    // foreign key is null'd
                    pocoForeign.SetValue(poco, val, null);
                }
                
            }


            IEnumerable<PropertyInfo> piCollection =
                poco.GetType().GetProperties()
                .Where(x => x.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(x.PropertyType));


            foreach (PropertyInfo item in piCollection)
            {
                PropertyInfo px = poco.GetType().GetProperty(item.Name, BindingFlags.Public | BindingFlags.Instance);

                // same property exists from dto to poco
                if (px != null)
                {
                    IList col = (IList)px.GetValue(poco, null);
                    if (col == null) continue;

                    Type dtoType = item.PropertyType.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>)).Single().GetGenericArguments()[0];

                    foreach (object elem in col)
                    {
                        db.AssignStub(elem);
                    }

                }
            }

        }//AssignStub



        internal static object LoadStub(Type t, string primaryKeyName, object id, DbContext db)
        {
            


            var cachedEnt =
                    db.ChangeTracker.Entries().Where(x => ObjectContext.GetObjectType(x.Entity.GetType()) == t).SingleOrDefault(x =>
                    {
                        Type entType = x.Entity.GetType();
                        object value = entType.InvokeMember(primaryKeyName, System.Reflection.BindingFlags.GetProperty, null, x.Entity, new object[] { });

                        return value.Equals(id);
                    });

                    

            if (cachedEnt != null)
            {
                return cachedEnt.Entity;
            }
            else
            {
                object stub = Activator.CreateInstance(t);


                t.InvokeMember(primaryKeyName, System.Reflection.BindingFlags.SetProperty, null, stub, new object[] { id });

                db.Entry(stub).State = EntityState.Unchanged;                               

                return stub;
            }


        }//LoadStub

    }
}
