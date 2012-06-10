using System;
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

        

        void MapCommon<TDtoProperty,TPocoProperty>(Expression<Func<TDto, TDtoProperty>> dtoProperty, Expression<Func<TPoco, TPocoProperty>> dtoSource, bool isKey)
            where TPocoProperty : TDtoProperty    
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

        public void Map<TDtoProperty,TPocoProperty>(Expression<Func<TDto, TDtoProperty>> propertyDestination, Expression<Func<TPoco, TPocoProperty>> propertySource)
            where TPocoProperty : TDtoProperty    
        {
            MapCommon(propertyDestination, propertySource, false);
        }

        public void MapKey<TDtoProperty,TPocoProperty>(Expression<Func<TDto, TDtoProperty>> propertyDestination, Expression<Func<TPoco, TPocoProperty>> propertySource)
            where TPocoProperty : TDtoProperty
        {
            MapCommon(propertyDestination, propertySource, true);
        }


        


        public void MapList<TDestElem, TSourceElem,TRef>(
            Expression<Func<TDto, IList<TDestElem>>> collectionDto,
            Expression<Func<TPoco, IList<TSourceElem>>> collectionPoco,
            Expression<Func<TSourceElem, TRef>> referencingObject) where TRef : TPoco
        {
            MapListCommon(collectionDto, collectionPoco, referencingObject.GetExpressionText());
        }

        public void MapList<TDestElem, TSourceElem>(
            Expression<Func<TDto, IList<TDestElem>>> collectionDto,
            Expression<Func<TPoco, IList<TSourceElem>>> collectionPoco) 
        {
            MapListCommon(collectionDto, collectionPoco, null);
        }

        void MapListCommon<TDestElem, TSourceElem>(
            Expression<Func<TDto, IList<TDestElem>>> collectionDto,
            Expression<Func<TPoco, IList<TSourceElem>>> collectionPoco,
            string referencingObject) 
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
                        ReferencingObject = referencingObject
                    }
                    );
            }

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
            foreach (PropertyInfo pi in dto.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
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

                        
                        // Collection mapping for DTO-to-POCO supports one level only
                        if (cm.CollectionPoco.Length > 1) continue;

                        PropertyInfo propPocoCol = poco.GetType().GetProperty(cm.CollectionPoco[0], BindingFlags.Public | BindingFlags.Instance);
                        Type pocoElemType = propPocoCol.PropertyType.GetGenericArguments()[0];


                        PropertyInfo pocoElemReferencingObject = cm.ReferencingObject != null ? pocoElemType.GetProperty(cm.ReferencingObject, BindingFlags.Public | BindingFlags.Instance) : null;
                        

                        IList dtoCol = ((IList)val);
                        
                        IList pocoCol = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(pocoElemType));


                        foreach (object item in dtoCol)
                        {
                            object pocoElem = Activator.CreateInstance(pocoElemType);
                            ToPoco(item, pocoElem);

                            if (pocoElemReferencingObject != null)
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
            ToDto(poco, dto, null);
        }

        static void ToDto(object poco, object dto, Type sourceType)
        {
            


            foreach (PropertyInfo pi in poco.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {


                // avoid circular reference
                if (sourceType != null)
                {
                    
                    // http://connect.microsoft.com/VisualStudio/feedback/details/679399/entity-framework-4-1-using-lazyloading-with-notracking-option-causes-invalidoperationexception
                    // See the Language's Countries in this project and the Country's Languages

                                        
                    bool isPropertyCollection = pi.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(pi.PropertyType);
                    if (isPropertyCollection)
                    {
                        if (pi.PropertyType.GetGenericArguments()[0] == sourceType)
                        {
                            continue;
                        }
                    }
                }

                
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

                    isOverriden = pm.Any(x => x.PropertyPoco.Length == 1);

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
                }//if



                
                {
                    bool isCollection = false;
                    Type collectionParent = null; //  e.g. Country.Languages. The languages parent is Country
                    


                    // pi.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(pi.PropertyType);
                    PropertyInfo propDto = null;


                    if (maps.ContainsKey(dto.GetType()))
                    {
                        MapSetting ms = maps[dto.GetType()];



                        // [0] means first level only
                        // First level is like this:
                        // MapCollectionLink(d => d.Koments, s => s.Comments, r => r.CommentId);
                        // Nested level is like this:
                        // MapCollectionLink(d => d.Koments, s => s.FavoriteBand.Comments, r => r.CommentId);

                        // Shall support obtaining the collection from nested level on later version



                        CollectionMapping cm =
                                ms.colMaps.Where(x => x.Value.CollectionPoco[0] == pi.Name)
                                            .Select(y => y.Value).SingleOrDefault();

                        // is collection overridden
                        if (cm != null)
                        {
                            // tentatively
                            isCollection = true;

                            object valWalk = val;

                            // e.g. Customer.Country.Languages
                            // first level is Customer
                            PropertyInfo levelWalk = pi;
                            foreach (string nextLevel in cm.CollectionPoco.Skip(1))
                            {
                                // no sense to walk further down the chain
                                if (valWalk == null)
                                {
                                    isCollection = false;
                                    break;
                                }

                                // save the parent first before drilling to child.
                                // use for avoiding circular reference:
                                // http://connect.microsoft.com/VisualStudio/feedback/details/679399/entity-framework-4-1-using-lazyloading-with-notracking-option-causes-invalidoperationexception
                                collectionParent = levelWalk.PropertyType;

                                levelWalk = levelWalk.PropertyType.GetProperty(nextLevel);
                                valWalk = levelWalk.GetValue(valWalk, null);
                            }

                            if (isCollection)
                            {                                
                                propDto = dto.GetType().GetProperty(cm.CollectionDto, BindingFlags.Public | BindingFlags.Instance);
                                val = valWalk;
                            }
                        }

                        // isCollection = true;
                    }

                    if (!isCollection && !isOverriden)
                        propDto = dto.GetType().GetProperty(pi.Name, BindingFlags.Public | BindingFlags.Instance);




                    // if property is existing
                    if (propDto != null)
                    {

                        if (!isCollection && !isOverriden)
                            propDto.SetValue(dto, val, null);
                        else if (val != null)
                        {
                            IList srcCollections = ((IList)val);

                            Type elemType = propDto.PropertyType.GetGenericArguments()[0];                            
                            IList clonedList = (IList) Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(elemType));

                            foreach (object item in srcCollections)
                            {
                                object dtoObject = Activator.CreateInstance(elemType);

                                // avoids circular reference
                                // http://connect.microsoft.com/VisualStudio/feedback/details/679399/entity-framework-4-1-using-lazyloading-with-notracking-option-causes-invalidoperationexception
                                ToDto(item, dtoObject, collectionParent);
                                clonedList.Add(dtoObject);
                            }
                            propDto.SetValue(dto, clonedList, null);
                        }
                    }//property is existing
                }

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
