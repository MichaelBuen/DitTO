﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;

namespace Ienablemuch.DitTO
{


    class CollectionMapping
    {
        public string CollectionDto { get; set; }
        public string[] CollectionPoco { get; set; }
        public string ReferencingObject { get; set; }
    }

    public class PropertyMapping
    {
        public string PropertyDto { get; set; }

        public Type PocoType { get; set; }
        public string[] PropertyPoco { get; set; }        
        public bool IsKey { get; set; }
    }



    class MapSetting
    {
        internal IDictionary<string, PropertyMapping> propMaps = new Dictionary<string, PropertyMapping>();
        internal IDictionary<string, CollectionMapping> colMaps = new Dictionary<string, CollectionMapping>();
    }



    public abstract class DtoPocoMap<TDto, TPoco> where TDto : new()
    {



        
        public DtoPocoMap()
        {
            bool isAlreadyMapped = Helper.maps.ContainsKey(typeof(TDto));  
            if (!isAlreadyMapped)
            {
                Mapping();
                isAlreadyMapped = true;
            }            
        }
        
        public abstract void Mapping();
        

        void MapCommon<TProperty>(Expression<Func<TDto, TProperty>> dtoProperty, Expression<Func<TPoco, TProperty>> dtoSource, bool isKey)
        {
            string dto = dtoProperty.GetExpressionText();

            if (!Helper.maps.ContainsKey(typeof(TDto)))
                Helper.maps.Add(typeof(TDto), new MapSetting());
            
            Helper.maps[typeof(TDto)].propMaps[dto] =
                new PropertyMapping
                {
                    PropertyDto = dto,
                    PocoType = typeof(TPoco),
                    PropertyPoco = dtoSource.GetExpressionArray(),
                    IsKey = isKey
                };
        }

        public void Map<TProperty>(Expression<Func<TDto, TProperty>> propertyDestination, Expression<Func<TPoco, TProperty>> propertySource)
        {
            MapCommon(propertyDestination, propertySource, false);
        }

        public void MapKey<TProperty>(Expression<Func<TDto, TProperty>> propertyDestination, Expression<Func<TPoco, TProperty>> propertySource)
        {
            MapCommon(propertyDestination, propertySource, true);
        }


        


        public void MapCollectionLink<TDestElem, TSourceElem,TRef>(
            Expression<Func<TDto, IList<TDestElem>>> collectionDto,
            Expression<Func<TPoco, IList<TSourceElem>>> collectionPoco,
            Expression<Func<TSourceElem, TRef>> referencingObject) where TRef : TPoco
        {
            string dtoColName = collectionDto.GetExpressionText();

            if (!Helper.maps.ContainsKey(typeof(TDto)))
                Helper.maps.Add(typeof(TDto), new MapSetting());


            if (!Helper.maps[typeof(TDto)].colMaps.ContainsKey(dtoColName))
            {
                
                Helper.maps[typeof(TDto)].colMaps.Add(dtoColName,
                    new CollectionMapping
                    {
                        CollectionDto = dtoColName,
                        CollectionPoco = collectionPoco.GetExpressionArray(),
                        ReferencingObject = referencingObject.GetExpressionText()
                    }
                    );
            }
            
            //Helper.maps[typeof(TDto)].colMaps[dtoColName] = 
            //    new CollectionMapping
            //    {
            //    };
        }


    }


    internal static class Helper
    {
        internal static string GetExpressionText(this LambdaExpression m)
        {

            string s = m.Body.ToString();
            string expressionOnly = string.Join(".", s.Split('.').Skip(1));
            return expressionOnly;

            // While we haven't yet grok the ASP.NET MVC's GetExpressionText to transform LambdaExpression to string only, 
            // the above will suffice for the meantime
        }

        internal static string[] GetExpressionArray(this LambdaExpression m)
        {

            string s = m.Body.ToString();
            return s.Split('.').Skip(1).ToArray();
           

            // While we haven't yet grok the ASP.NET MVC's GetExpressionText to transform LambdaExpression to string only, 
            // the above will suffice for the meantime
        }


        internal static void ToPoco(object dto, object poco)
        {
            foreach (PropertyInfo pi in dto.GetType().GetProperties())
            {
                object val = pi.GetValue(dto, null);
                if (val == null) continue;


                PropertyMapping pm = null;
                if (maps.ContainsKey(dto.GetType()))
                {                    
                    MapSetting ms = maps[dto.GetType()];

                    pm =
                        ms.propMaps.Where(x => x.Value.PropertyDto == pi.Name)
                        .Select(y => y.Value).SingleOrDefault();

                    // found an override mapping, yet it's not a key,
                    // ignore it (via continue).
                    // e.g. Map(x => x.Address1, y => y.Customer.Address1);
                    // This is not what to ignore:
                    // e.g. MapKey(x => x.CustomerId, y => y.Customer.CustomerId);

                    if (pm != null && !pm.IsKey) continue;
                    

                }

                // overridden mapping
                if (pm != null)
                {
                    
                    
                    object valDefault = val.GetType().GetDefault();

                    // if 0, empty string, Guid 00000-000000 (?)
                    if (object.Equals(valDefault, val)) continue;

                    // PropertySource[0]
                    // e.g. Customer of Customer.CustomerId                    
                    string pocoPropertyName = pm.PropertyPoco.First();
                    PropertyInfo pPocoProperty = poco.GetType().GetProperty(pocoPropertyName, BindingFlags.Public | BindingFlags.Instance);
                    


                    // impossibility, given we are using strongly-typed
                    if (pPocoProperty == null) throw new PocoMappingException(string.Format("POCO {0} not found for {1}.{2}", pm.PropertyPoco[0], dto.GetType(), pi.Name));

                    // e.g. Customer.CustomerId. Last is CustomerId
                    // POCO referencing property
                    string pocoRef = pm.PropertyPoco.Last();                    
                    PropertyInfo pPocoPropertyId = pPocoProperty.PropertyType.GetProperty(pocoRef, BindingFlags.Public | BindingFlags.Instance);

                    // impossibility, given we are using strongly-typed property
                    if (pPocoPropertyId == null)
                        // throw new PocoMappingException(string.Format("POCO Property {0}.{1} not found for {2}.{3}", pm.PropertySource[0], pocoRef, dto.GetType(), pi.Name));
                        throw new PocoMappingException("not found");

                    // e.g. Customer of Customer.CustomerId
                    object pocoProperty = Activator.CreateInstance(pPocoProperty.PropertyType);

                    // e.g. val = 76. val goes to CustomerId of Customer.CustomerId
                    // if (pocoRef == "CustomerId") throw new Exception("val : " + val + " " + pocoRefObject.GetType() + " : " + dstPocoProperty.PropertyType + " " + dstPocoProperty.Name);

                    
                    pPocoPropertyId.SetValue(pocoProperty, val, null);

                    

                    // e.g. poco = Order, pocoRefObject = Customer
                    // if (pocoRef == "CustomerId") throw new Exception(dstPoco.PropertyType.ToString() + " xx " + poco.GetType() + " -- " + dstPoco);

                    // if (pocoRef == "CustomerId") throw new Exception(pocoRefObject.GetType() + " " + pocoRefObject + " xxx " + propPoco);
                    
                    

                    // if (propPoco == null) throw new Exception(pi.Name + " " + pocoRef + "x " + pm.PropertyPoco.First());
                    pPocoProperty.SetValue(poco, pocoProperty, null);

                    

                }
                else // non-overridden
                {
                    PropertyInfo propPoco = poco.GetType().GetProperty(pi.Name, BindingFlags.Public | BindingFlags.Instance);

                    bool isCollection = pi.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(pi.PropertyType);


                    if (!isCollection)
                    {
                        // if property is non-existing
                        if (propPoco == null) continue;
                        propPoco.SetValue(poco, val, null);
                    }
                    else
                    {
                        // need an explicit mapping

                        if (!maps.ContainsKey(dto.GetType())) continue;
                        
                        MapSetting ms = maps[dto.GetType()];
                        if (!ms.colMaps.ContainsKey(pi.Name)) continue;

                        CollectionMapping cm = ms.colMaps[pi.Name];

                        

                        // Collection supports one level only
                        PropertyInfo propPocoCol = poco.GetType().GetProperty(cm.CollectionPoco[0], BindingFlags.Public | BindingFlags.Instance);
                        Type pocoElemType = propPocoCol.PropertyType.GetGenericArguments()[0];


                        PropertyInfo pocoElemReferencingObject = 
                            pocoElemType.GetProperty(cm.ReferencingObject, BindingFlags.Public | BindingFlags.Instance);


                        IList dtoCol = ((IList)val);

                        IList pocoCol = (IList)Common.Create("System.Collections.Generic.List", pocoElemType);

                        foreach (object item in dtoCol)
                        {
                            object pocoElem = Activator.CreateInstance(pocoElemType);
                            ToPoco(item, pocoElem);

                            pocoElemReferencingObject.SetValue(pocoElem, poco, null);

                            pocoCol.Add(pocoElem);
                        }

                        propPocoCol.SetValue(poco, pocoCol, null);

                    }
                }
                


            }
        }//void

        internal static void ToDto(object poco, object dto)
        {
            foreach (PropertyInfo pi in poco.GetType().GetProperties())
            {
                object val = pi.GetValue(poco, null);
                if (val == null) continue;


                bool isOverriden = false;

                
                if (maps.ContainsKey(dto.GetType()))
                {
                    MapSetting ms = maps[dto.GetType()];


                    // get all with similar object
                    // e.g. Customer.CustomerId, Customer.CustomerName, Customer.YearMember. They are similar Customer,
                    // so to make assigning efficient
                    IEnumerable<PropertyMapping> pm =
                        ms.propMaps.Where(x => x.Value.PropertyPoco[0] == pi.Name)
                            .Select(y => y.Value);

                    isOverriden = pm.Count() != 0;

                    foreach (PropertyMapping p in pm)
                    {
                        // e.g. Customer

                        object obtainedVal = val;

                        // e.g. Customer.Country.CountryName
                        foreach (string prop in p.PropertyPoco.Skip(1))
                        {
                            // e.g. Country.  On next loop: CountryName
                            Type pocoType = obtainedVal.GetType();
                            PropertyInfo pocoProp = pocoType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
                            obtainedVal = pocoProp.GetValue(obtainedVal, null);

                            if (obtainedVal == null) break;
                        }

                        if (obtainedVal == null) continue;

                        PropertyInfo px = dto.GetType().GetProperty(p.PropertyDto);
                        px.SetValue(dto, obtainedVal, null);
                    }
                }



                if (!isOverriden)
                {
                    bool isCollection = pi.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(pi.PropertyType);


                    PropertyInfo propDto = dto.GetType().GetProperty(pi.Name, BindingFlags.Public | BindingFlags.Instance);
                    
                    if (isCollection)
                    {       
                        if (maps.ContainsKey(dto.GetType()))
                        {                            
                            MapSetting ms = maps[dto.GetType()];
             


                            // [0] means first level only
                            // First level is like this:
                            // MapCollectionLink(d => d.Koments, s => s.Comments, r => r.CommentId);
                            // Nested level is like this:
                            // MapCollectionLink(d => d.Koments, s => s.FavoriteBand.Comments, r => r.CommentId);

                            // Shalling support obtaining the collection from nested level on later version
                            CollectionMapping cm =
                                    ms.colMaps.Where(x => x.Value.CollectionPoco[0] == pi.Name)
                                                .Select(y => y.Value).SingleOrDefault();

                            // is collection overridden
                            if (cm != null)
                            {
                                // throw new Exception("Great" + pi.Name);
                                propDto = dto.GetType().GetProperty(cm.CollectionDto, BindingFlags.Public | BindingFlags.Instance);
                            }

                        }
                        
                    }



                    // if property is existing
                    if (propDto != null)
                    {
                        
                        if (!isCollection)
                            propDto.SetValue(dto, val, null);
                        else
                        {
                            IList srcCollections = ((IList)val);

                            Type elemType = propDto.PropertyType.GetGenericArguments()[0];
                            IList clonedList = (IList)Common.Create("System.Collections.Generic.List", elemType);

                            foreach (object item in srcCollections)
                            {
                                object dtoObject = Activator.CreateInstance(elemType);
                                ToDto(item, dtoObject);
                                clonedList.Add(dtoObject);
                            }
                            propDto.SetValue(dto, clonedList, null);
                        }
                    }//property is existing
                }//if

            }//foreach            
        }//void

        internal static IDictionary<Type, MapSetting> maps = new Dictionary<Type, MapSetting>();


    }

    public static class Mapper
    {
        static Mapper()
        {
            
        }

        public static void FromAssemblyOf<T>()
        {
            // IEnumerable<Type> mappings = Assembly.GetCallingAssembly()

            IEnumerable<Type> mappings = typeof(T).Assembly            
                .GetTypes().Where(x => 
                    x.BaseType.IsGenericType 

                    // this is possible too:
                    // &&  typeof(DtoPocoMap<,>).IsAssignableFrom(x.BaseType.GetGenericTypeDefinition())

                    && x.BaseType.GetGenericTypeDefinition() == typeof(DtoPocoMap<,>)

                    );
                

            
            foreach (Type t in mappings)
            {
                Activator.CreateInstance(t);
            }
        }


        public static IEnumerable<PropertyMapping> StubsNeeded(Type tpoco)
        {

            /*bool hasMapping = Helper.maps.ContainsKey(tpoco);
            return Helper.maps[tpoco].propMaps.Values.Where(x => x.IsKey);*/

            var z = from x in Helper.maps
                    from p in x.Value.propMaps
                    where p.Value.PocoType == tpoco && p.Value.IsKey
                    select p.Value;

            return z;
        }



        public static TDto ToDto<TPoco,TDto>(TPoco poco) where TDto : new()
        {
            TDto dto = new TDto();
            Helper.ToDto(poco, dto);
            return dto;
        }

        public static TPoco ToPoco<TDto,TPoco>(TDto dto) where TPoco : new()
        {
            TPoco poco = new TPoco();
            Helper.ToPoco(dto, poco);
            return poco;
        }

    }

    public class PocoMappingException : Exception
    {
        public PocoMappingException() : base() { }

        public PocoMappingException(string message) : base(message) { }

        public PocoMappingException(string message, Exception innerException)
            : base(message, innerException)
        {
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

        internal static object GetDefault(this Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }


}
