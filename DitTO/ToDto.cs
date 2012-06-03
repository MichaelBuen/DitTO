using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using System.Collections;

namespace Ienablemuch.DitTO
{

    public static class Poco
    {
        public static TDst ToDto<TDst>(object pocoSource) where TDst : new()
        {
            TDst dst = new TDst();
            ToDto(pocoSource, dst);
            return dst;
        }


        static void ToDto(object src, object dst)
        {                             
            foreach (PropertyInfo pi in src.GetType().GetProperties())
            {
                object val = pi.GetValue(src, null);
                if (val == null) continue;

                PropertyInfo propDst = dst.GetType().GetProperty(pi.Name, BindingFlags.Public | BindingFlags.Instance);

                // if property is existing
                if (propDst != null)
                {
                    bool isCollection = pi.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(pi.PropertyType);                    
                    if (!isCollection)
                        propDst.SetValue(dst, val, null);
                    else 
                    {
                        IList srcCollections = ((IList)val);                        

                        Type elemType = propDst.PropertyType.GetGenericArguments()[0];
                        IList clonedList = (IList)Common.Create("System.Collections.Generic.List", elemType);                        

                        foreach (object item in srcCollections)
                        {
                            object dtoObject = Activator.CreateInstance(elemType);
                            ToDto(item, dtoObject);
                            clonedList.Add(dtoObject);
                        }
                        propDst.SetValue(dst, clonedList, null);                        
                    }
                }
                else
                {
                    IEnumerable<PropertyInfo> pocoMatchProperties = 
                        dst.GetType().GetProperties()
                            .Where(x => 
                                x.GetCustomAttributes(typeof(PocoMappingAttribute),false)
                                .OfType<PocoMappingAttribute>().Any(y => y.PocoName == pi.Name)
                                );

                    foreach (PropertyInfo item in pocoMatchProperties)
                    {
                        PocoMappingAttribute pm = item.GetCustomAttributes(typeof(PocoMappingAttribute), false).OfType<PocoMappingAttribute>().Single();

                        Type pocoType = val.GetType();
                        PropertyInfo pocoProp = pocoType.GetProperty(pm.PropertyName, BindingFlags.Public | BindingFlags.Instance);

                        // property not existing on POCO
                        if (pocoProp == null) throw new PocoMappingException(string.Format("Property {0}.{1} not found for {2}", pi.Name, pm.PropertyName, pi.Name));

                        object valx = pocoProp.GetValue(val, null);
                        item.SetValue(dst, valx, null);                        
                    }//foreach


                }//if

            }//foreach            
        }//void


        


    }


    public static class Dto
    {
        public static TDst ToPoco<TDst>(object dtoSource) where TDst : new()
        {
            TDst dst = new TDst();
            ToPoco(dtoSource, dst);
            return dst;
        }

        static void ToPoco(object src, object dst)
        {
            foreach (PropertyInfo pi in src.GetType().GetProperties())
            {
                object val = pi.GetValue(src, null);
                if (val == null) continue;

                PocoMappingAttribute pm =
                    pi.GetCustomAttributes(typeof(PocoMappingAttribute), false)
                    .OfType<PocoMappingAttribute>().SingleOrDefault();

                if (pm != null && !pm.IsReference) continue;

               
                if (pm == null)
                {
                    PropertyInfo propDst = dst.GetType().GetProperty(pi.Name, BindingFlags.Public | BindingFlags.Instance);
                    

                    if (propDst != null)
                    {
                        bool isCollection = pi.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(pi.PropertyType);

                        if (!isCollection)
                            propDst.SetValue(dst, val, null);
                        else
                        {
                            Type elemType = propDst.PropertyType.GetGenericArguments()[0];

                            PocoCollectionLinkAttribute pcl = pi.GetCustomAttributes(typeof(PocoCollectionLinkAttribute), false).OfType<PocoCollectionLinkAttribute>().SingleOrDefault();
                            PropertyInfo linkProp;
                            if (pcl != null)
                                linkProp = elemType.GetProperty(pcl.PocoCollectionLink, BindingFlags.Public | BindingFlags.Instance);
                            else
                                linkProp = null;

                            IList srcCollections = ((IList)val);
                            
                            IList clonedList = (IList)Common.Create("System.Collections.Generic.List", elemType);

                            

                            foreach (object item in srcCollections)
                            {
                                object pocoObject = Activator.CreateInstance(elemType);
                                ToPoco(item, pocoObject);

                                // attribute and actual parent link are existing
                                if (pcl != null && linkProp != null)
                                {
                                    // throw new Exception(dst.ToString());
                                    linkProp.SetValue(pocoObject, dst, null);
                                }

                                clonedList.Add(pocoObject);
                            }
                            propDst.SetValue(dst, clonedList, null);                                       
                        }

                    }
                }
                else // pm != null && pm.IsKey
                {
                    object valDefault = val.GetType().GetDefault();


                    // if 0, empty string, Guid 00000-000000 (?)
                    if (object.Equals(valDefault, val)) continue;


                    PropertyInfo dstPoco = dst.GetType().GetProperty(pm.PocoName, BindingFlags.Public | BindingFlags.Instance);
                    

                    // e.g. OrderLine.Product
                    if (dstPoco == null) throw new PocoMappingException(string.Format("POCO {0} not found for {1}.{2}", pm.PocoName, src.GetType(), pi.Name));

                    PropertyInfo dstProperty = dstPoco.PropertyType.GetProperty(pm.PropertyName, BindingFlags.Public | BindingFlags.Instance);

                    // e.g. OrderLine.Product.ProductID
                    if (dstProperty == null) throw new PocoMappingException(string.Format("POCO Property {0}.{1} not found for {2}.{3}", pm.PocoName, pm.PropertyName, src.GetType(), pi.Name));
                    
                        
                    // e.g. OrderLine.Product
                    object propRefObject = Activator.CreateInstance(dstPoco.PropertyType);
                    

                    dstProperty.SetValue(propRefObject, val, null);
                    dstPoco.SetValue(dst, propRefObject, null);
                }

            }//foreach
        }
    }

    static class Common
    {
        internal static object Create(string name, params Type[] types)
        {
            string t = name + "`" + types.Length;
            Type generic = Type.GetType(t).MakeGenericType(types);
            return Activator.CreateInstance(generic);
        }

        public static object GetDefault(this Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }


    // [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct )]
    public class PocoMappingAttribute : Attribute
    {
        public string PocoName { get; set; }
        public string PropertyName { get; set; }
        public bool IsReference { get; set; }
        public PocoMappingAttribute(string pocoName, string propertyName)
            : this(pocoName, propertyName, false)
        {
            
        }

        public PocoMappingAttribute(string pocoName, string propertyName, bool isKey)
        {
            this.PocoName = pocoName;
            this.PropertyName = propertyName;
            this.IsReference = isKey;
        }
    }



    public class PocoCollectionLinkAttribute : Attribute
    {
        public string PocoCollectionLink { get; set; }

        public PocoCollectionLinkAttribute(string pocoCollectionLink)
        {
            this.PocoCollectionLink = pocoCollectionLink;
        }
    }

    public class PocoMappingException : Exception
    {
        public PocoMappingException() : this("")
        {

        }

        public PocoMappingException(string message) : base(message)
        {
            
        }

        public PocoMappingException(string message, Exception innerException) : base(message, innerException)
        {            
        }
    }
}
