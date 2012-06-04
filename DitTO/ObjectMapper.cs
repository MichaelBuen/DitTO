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
        public string CollectionDestination { get; set; }
        public string CollectionSource { get; set; }
        public string ReferencingObject { get; set; }
    }

    class PropertyMapping
    {
        public string PropertyDestination { get; set; }
        public string[] PropertySource { get; set; }
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
        

        void MapCommon<TProperty>(Expression<Func<TDto, TProperty>> propertyDestination, Expression<Func<TPoco, TProperty>> propertySource, bool isKey)
        {
            string dest = propertyDestination.GetExpressionText();

            if (!Helper.maps.ContainsKey(typeof(TDto)))
                Helper.maps.Add(typeof(TDto), new MapSetting());
            
            Helper.maps[typeof(TDto)].propMaps[dest] =
                new PropertyMapping
                {
                    PropertyDestination = dest,
                    PropertySource = propertySource.GetExpressionArray()

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


        


        public void MapCollectionLink<TDestElem, TSourceElem>(
            Expression<Func<TDto, IList<TDestElem>>> collectionDestination,
            Expression<Func<TPoco, IList<TSourceElem>>> collectionSource,
            Expression<Func<TSourceElem, object>> referencingObject)
        {
            string colDest = collectionDestination.GetExpressionText();


            Helper.maps[typeof(TPoco)].colMaps[colDest] = 
                new CollectionMapping
                {
                    CollectionDestination = colDest,
                    CollectionSource = collectionSource.GetExpressionText(),
                    ReferencingObject = referencingObject.GetExpressionText()
                };
            
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


        internal static void ToPoco(object poco, object dto)
        {

        }

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

                    IEnumerable<PropertyMapping> pm =
                        ms.propMaps.Where(x => x.Value.PropertySource[0] == pi.Name)
                            .Select(y => y.Value);

                    isOverriden = pm.Count() != 0;

                    foreach (PropertyMapping p in pm)
                    {
                        // e.g. Customer

                        object obtainedVal = val;

                        // e.g. Customer.Country.CountryName
                        foreach (string prop in p.PropertySource.Skip(1))
                        {
                            // e.g. Country.  On next loop: CountryName
                            Type pocoType = obtainedVal.GetType();
                            PropertyInfo pocoProp = pocoType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
                            obtainedVal = pocoProp.GetValue(obtainedVal, null);

                            if (obtainedVal == null) break;
                        }

                        if (obtainedVal == null) continue;

                        PropertyInfo px = dto.GetType().GetProperty(p.PropertyDestination);
                        px.SetValue(dto, obtainedVal, null);
                    }
                }

                if (!isOverriden)
                {
                    PropertyInfo propDto = dto.GetType().GetProperty(pi.Name, BindingFlags.Public | BindingFlags.Instance);

                    // if property is existing
                    if (propDto != null)
                    {
                        bool isCollection = pi.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(pi.PropertyType);
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

        public static TDto ToDto<TPoco,TDto>(TPoco poco) where TDto : new()
        {
            TDto dto = new TDto();
            Helper.ToDto(poco, dto);
            return dto;
        }

        public static TDto ToPoco<TDto,TPoco>(TDto dto) where TPoco : new()
        {
            TPoco poco = new TPoco();
            Helper.ToPoco(dto, poco);
            return dto;
        }

    }

}
