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
        public static void AssignStub<TDto>(object poco, DbContext db) where TDto : new()
        {
            // TDto x = new TDto();

            AssignStub(typeof(TDto), poco, db);
            

        }

        internal static void AssignStub(Type dtoPattern, object poco, DbContext db)
        {
            IEnumerable<PropertyInfo> piNeedStub =
                dtoPattern.GetProperties()
                .Where(x =>
                    x.GetCustomAttributes(typeof(PocoMappingAttribute), false)
                    .OfType<PocoMappingAttribute>()
                    .Any(z => z.IsReference)
                 );
                 

            foreach (PropertyInfo item in piNeedStub)
            {
                PocoMappingAttribute pm = item.GetCustomAttributes(typeof(PocoMappingAttribute),false).OfType<PocoMappingAttribute>().Single();

                PropertyInfo px = poco.GetType().GetProperty(pm.PocoName, BindingFlags.Public | BindingFlags.Instance);
                if (px == null) continue;
               
                
                object val = px.GetValue(poco, null);
                // foreign key nullable
                if (val == null) continue;



                PropertyInfo pxProp = val.GetType().GetProperty(pm.PropertyName, BindingFlags.Public | BindingFlags.Instance);

                object id = pxProp.GetValue(val, null);

                px.SetValue(poco, LoadStub(px.PropertyType, pm.PropertyName, id, db), null);               
            }


            IEnumerable<PropertyInfo> piCollection =
                dtoPattern.GetProperties()
                .Where(x => x.PropertyType.IsGenericType &&  typeof(IEnumerable).IsAssignableFrom(x.PropertyType));


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
                        //throw new Exception(dtoType.ToString());                        
                        AssignStub(dtoType, elem, db);
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
