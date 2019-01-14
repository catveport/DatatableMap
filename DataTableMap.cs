using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DataTableMap
{
    /// <summary>
    /// Simple mapper to convert Datatable objects to Entity Objects with their relations.
    /// These relations can be one to one relaition (required or not) and one to many relaitons.
    /// </summary>
    public static class DataTableMap
    {
        /// <summary>
        /// Extension method of datatable that return a list of entity objects.
        /// </summary>
        /// <typeparam name="TEntity">Type of the new entity.</typeparam>
        /// <param name="dataTable">Datatable that contain the data to be converted.</param>
        /// <param name="convertToPrimitive">Expecific class that define convertion to the diferents primitive types.</param>
        /// <returns>An IList of the new entity.</returns>
        public static IList<TEntity> MapToEntity<TEntity>(this DataTable dataTable, ConvertToPrimitive convertToPrimitive = null) where TEntity : class
        {
            return MapToEntity<TEntity>(dataTable, null, null, false);
        }

        /// <summary>
        /// Extension method of datatable that return a list of entity objects.
        /// </summary>
        /// <typeparam name="TEntity">Type of the new entity.</typeparam>
        /// <param name="dataTable">Datatable that contain the data to be converted.</param>
        /// <param name="dataTables">Array of Datatables that contain the data to be converted for the relation entities.</param>
        /// <param name="convertToPrimitive">Expecific class that define convertion to the diferents primitive types.</param>
        /// <param name="oneToOneRelationRequired">Boolean that indicated when the one to one relation are required.</param>
        /// <returns>An IList of the new entity.</returns>
        public static IList<TEntity> MapToEntity<TEntity>(this DataTable dataTable, DataTable[] dataTables, ConvertToPrimitive convertToPrimitive = null, bool oneToOneRelationRequired = false) where TEntity : class
        {
            return MapToEntity<TEntity>(dataTable, null, dataTables, oneToOneRelationRequired: oneToOneRelationRequired);
        }


        /// <summary>
        /// Extension method of datatable that return a list of entity objects.
        /// </summary>
        /// <typeparam name="TEntity">Type of the new entity.</typeparam>
        /// <param name="dataTable">Datatable that contain the data to be converted.</param>
        /// <param name="filter">Function that allows to filter data before be converted to the entities. It apply to the rows in the main datatable.</param>
        /// <param name="dataTables">Array of Datatables that contain the data to be converted for the relation entities.</param>
        /// <param name="convertToPrimitive">Expecific class that define convertion to the diferents primitive types.</param>
        /// <param name="oneToOneRelationRequired">Boolean that indicated when the one to one relation are required.</param>
        /// <returns>An IList of the new entity.</returns>
        public static IList<TEntity> MapToEntity<TEntity>(this DataTable dataTable, Func<DataTable, DataRow[]> filter, DataTable[] dataTables = null, ConvertToPrimitive convertToPrimitive = null, bool oneToOneRelationRequired = false) where TEntity : class
        {
            if (dataTable == null)
                throw new ArgumentNullException("Datatable is Null");

            var datarows = dataTable.Select();

            if (datarows == null)
                throw new ArgumentNullException("Datarows is Null");

            if (filter != null)
            {
                datarows = filter(dataTable);
            }

            IList<TEntity> entities = new List<TEntity>();
            try
            {
                for (int i = 0; i < datarows.Length; i++)
                {
                    entities.Add(GetEntity<TEntity>(datarows[i], dataTables, convertToPrimitive, null, oneToOneRelationRequired));
                }
                return entities;
            }
            catch (Exception ex)
            {
                throw new Exception("Error mapping entity", ex);
            }
        }

        private static TEntity GetEntity<TEntity>(DataRow dataRow, DataTable[] dataTables, ConvertToPrimitive convertToPrimitive, object parentObj, bool oneToOneRelationRequired)
        {
            if (dataRow == null)
                throw new ArgumentNullException();

            TEntity obj = Activator.CreateInstance<TEntity>();

            //get entity values for all properties mapped that they don't have an one to one or one to many attribute. 
            GetEntityValues(dataRow, convertToPrimitive, obj);

            if (dataTables != null && dataTables.Any())
            {
                //get all the one to one relational entities.
                GetOneToOneEntity(dataTables, convertToPrimitive, obj, oneToOneRelationRequired);

                //get all the one to many relational entities.
                GetOneToManyEntities(dataTables, convertToPrimitive, obj);
            }

            //set the navegation property
            if (parentObj != null)
            {
                var propertyInfo = typeof(TEntity).GetProperties()
                    .Where(p => p.PropertyType == parentObj.GetType() && p.GetCustomAttributes(typeof(ParentNavigation), true).Any()).SingleOrDefault();
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(obj, parentObj, null);
                }
            }

            return (TEntity)obj;
        }

        private static void GetEntityValues<TEntity>(DataRow dataRow, ConvertToPrimitive convertToPrimitive, TEntity obj)
        {
            var properties = typeof(TEntity).GetProperties()
                            .Where(p => !p.GetCustomAttributes(typeof(NotMapped), true).Any() &&
                                         !p.GetCustomAttributes(typeof(OneToManyRelation), true).Any() &&
                                         !p.GetCustomAttributes(typeof(OneToOneRelation), true).Any()
                            );

            var colunmsNames = dataRow.Table.Columns;

            if (properties != null && properties.Any())
            {
                ConvertToPrimitive convert = convertToPrimitive != null ? convertToPrimitive : new ConvertToPrimitive();

                foreach (var prop in properties)
                {
                    var attr = prop.GetCustomAttributes(typeof(ColumnName), true);
                    string name = attr.Any() ? (attr.Single() as ColumnName).Name : prop.Name;

                    if(dataRow.Table.Columns.Contains(name) && dataRow[name] != DBNull.Value)
                    { 
                        var value = dataRow[name];
                        convert.Convert<TEntity>(prop, obj, value);
                    }
                }
            }
        }

        private static void GetOneToOneEntity<TEntity>(DataTable[] dataTables, ConvertToPrimitive convertToPrimitive, TEntity parentObj, bool oneToOneRelationRequired)
        {
            var type = typeof(TEntity);
            var propertiesOneToOne = type.GetProperties().Where(p => !p.GetCustomAttributes(typeof(NotMapped), true).Any() &&
                            p.GetCustomAttributes(typeof(OneToOneRelation), true).Any());

            var propertiesNames = type.GetProperties().Where(p => !p.GetCustomAttributes(typeof(NotMapped), true).Any() &&
                             p.GetCustomAttributes(typeof(ColumnName), true).Any());

            var propertiesNamesObj = type.GetProperties().Where(p => !p.GetCustomAttributes(typeof(NotMapped), true).Any() &&
                            !p.GetCustomAttributes(typeof(OneToOneRelation), true).Any() &&
                            !p.GetCustomAttributes(typeof(OneToManyRelation), true).Any() &&
                            !p.GetCustomAttributes(typeof(ColumnName), true).Any());

            if (propertiesOneToOne != null && propertiesOneToOne.Any())
            {
                foreach (var prop in propertiesOneToOne)
                {
                    var oneToOneAttrs = prop.GetCustomAttributes(typeof(OneToOneRelation), true);
                    
                    if (oneToOneAttrs.Count() > 1)
                        throw new System.InvalidOperationException(string.Format("Property {0} contains a duplicate attribute", prop.Name));

                    OneToOneRelation attr = oneToOneAttrs.Single() as OneToOneRelation;

                    DataTable datatableOneToOne;

                    if (oneToOneRelationRequired || attr.Required)
                    {
                        var datat = dataTables.Where(dt => string.Equals(dt.TableName, attr.TableName, StringComparison.OrdinalIgnoreCase));

                        if (!datat.Any())
                            throw new ArgumentNullException(string.Format("Required Datatable was not found for the property {0} with an OneToOneRelation attribute (DataTable:{1})", prop.Name, attr.TableName));

                        datatableOneToOne = datat.First();
                    }
                    else
                        datatableOneToOne = dataTables.Where(dt => string.Equals(dt.TableName, attr.TableName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                    if (datatableOneToOne != null && datatableOneToOne.Rows.Count > 0)
                    {
                        PropertyInfo propertyParentKey = null;
                        Dictionary<string, PropertyInfo> propertyParentKeys = new Dictionary<string, PropertyInfo>();

                        if (!string.IsNullOrWhiteSpace(attr.ParentKey))
                        {
                            propertyParentKey = SearchPropertyParent(propertiesNames, propertiesNamesObj, attr.ParentKey);
                        }
                        else if (attr.ParentKeys != null && attr.ParentKeys.Any())
                        {
                            foreach (var key in attr.ParentKeys)
                            {
                                var resul = SearchPropertyParent(propertiesNames, propertiesNamesObj, key);
                                propertyParentKeys.Add(key, resul);
                            }
                        }
                        else
                        {
                            throw new ArgumentException("One to one relational attributes are required");
                        }

                        Func<DataRow, bool> funcOneToOne = null;

                        if (!string.IsNullOrWhiteSpace(attr.SiblingKey) && !string.IsNullOrWhiteSpace(attr.ParentKey))
                        {
                            if(propertyParentKey == null)
                                throw new ArgumentException(string.Format("One to one relational attributes are required, ParentKey ({0}) property was not found", attr.ParentKey));

                            funcOneToOne = (r) =>
                            {
                                if (r.Table.Columns.Contains(attr.SiblingKey))
                                    return string.Equals(r[attr.SiblingKey].ToString(), propertyParentKey.GetValue(parentObj, null).ToString(), StringComparison.OrdinalIgnoreCase);
                                else
                                    throw new ArgumentException(string.Format("One to one relational attributes are required, SiblingKey ({0}) column was not found", attr.SiblingKey));
                            };
                        }
                        else if (attr.SiblingKeys != null && attr.SiblingKeys.Any() && propertyParentKeys != null && propertyParentKeys.Any() && !propertyParentKeys.Any(p => p.Value == null)
                            && attr.SiblingKeys.Count() == propertyParentKeys.Count())
                        {
                            funcOneToOne = (r) =>
                            {
                                bool resul = false;
                                for (int i = 0; i < attr.SiblingKeys.Length; i++)
                                {
                                    if (propertyParentKeys.TryGetValue(attr.ParentKeys[i], out propertyParentKey))
                                    {
                                        if (r.Table.Columns.Contains(attr.SiblingKeys[i]))
                                            resul = string.Equals(r[attr.SiblingKeys[i]].ToString(), propertyParentKey.GetValue(parentObj, null).ToString(), StringComparison.OrdinalIgnoreCase);
                                        else
                                            throw new ArgumentException(string.Format("One to one relational attributes are required, SiblingKey ({0}) column was not found", attr.SiblingKeys[i]));
                                    }
                                    else
                                    {
                                        throw new ArgumentException("One to one relational attributes must match");
                                    }
                                    if (!resul) break;
                                }
                                return resul;
                            };
                        }
                        else
                        {
                            throw new ArgumentException("One to one relational attributes are required");
                        }

                        var dataRows = datatableOneToOne.Select().Where(funcOneToOne);

                        if (dataRows.Count() > 1)
                            throw new System.InvalidOperationException("Only one row must match in an One to One Relation");

                        var dataRow = dataRows.SingleOrDefault();

                        if ((oneToOneRelationRequired || attr.Required) && dataRow == null)
                            throw new NullReferenceException("One to one relational row is required");
                        else if (dataRow == null)
                            continue;

                        MethodInfo method = typeof(DataTableMap).GetMethod("GetEntity", BindingFlags.Static | BindingFlags.NonPublic);
                        MethodInfo generic = method.MakeGenericMethod(new[] { prop.PropertyType });

                        var obj = generic.Invoke(null, new object[] { dataRow, dataTables, convertToPrimitive, parentObj, oneToOneRelationRequired });
                        prop.SetValue(parentObj, obj, null);
                    }
                    else if (oneToOneRelationRequired || attr.Required)
                    {
                        throw new ArgumentException("One to one relational table is required");
                    }
                }
            }
        }

        private static void GetOneToManyEntities<TEntity>(DataTable[] dataTables, ConvertToPrimitive convertToPrimitive, TEntity parentObj)
        {
            var type = typeof(TEntity);
            var propertiesOneToMany = type.GetProperties().Where(p => !p.GetCustomAttributes(typeof(NotMapped), true).Any() &&
                            p.GetCustomAttributes(typeof(OneToManyRelation), true).Any()); ///

            var propertiesNames = type.GetProperties().Where(p => !p.GetCustomAttributes(typeof(NotMapped), true).Any() &&
                             p.GetCustomAttributes(typeof(ColumnName), true).Any());

            var propertiesNamesObj = type.GetProperties().Where(p => !p.GetCustomAttributes(typeof(NotMapped), true).Any() &&
                            !p.GetCustomAttributes(typeof(OneToOneRelation), true).Any() &&
                            !p.GetCustomAttributes(typeof(OneToManyRelation), true).Any() &&
                            !p.GetCustomAttributes(typeof(ColumnName), true).Any());

            if (propertiesOneToMany != null && propertiesOneToMany.Any())
            {
                foreach (var prop in propertiesOneToMany)
                {
                    var oneToManyAttrs = prop.GetCustomAttributes(typeof(OneToManyRelation), true);

                    if (oneToManyAttrs.Count() > 1)
                        throw new System.InvalidOperationException(string.Format("Property {0} contains a duplicate attribute", prop.Name));

                    OneToManyRelation attr = oneToManyAttrs.Single() as OneToManyRelation;

                    var datatableOneToMany = dataTables.Where(dt => string.Compare(dt.TableName, attr.TableName, true) == 0).FirstOrDefault();

                    if (datatableOneToMany != null && datatableOneToMany.Rows.Count > 0)
                    {
                        PropertyInfo propertyParentKey = null;
                        Dictionary<string, PropertyInfo> propertyParentKeys = new Dictionary<string, PropertyInfo>();

                        if (!string.IsNullOrWhiteSpace(attr.ParentKey))
                        {
                            propertyParentKey = SearchPropertyParent(propertiesNames, propertiesNamesObj, attr.ParentKey);
                        }
                        else if (attr.ParentKeys != null && attr.ParentKeys.Any())
                        {
                            foreach (var key in attr.ParentKeys)
                            {
                                var resul = SearchPropertyParent(propertiesNames, propertiesNamesObj, key);
                                propertyParentKeys.Add(key, resul);
                            }
                        }
                        else
                        {
                            throw new ArgumentException("One to Many relational attributes are required");
                        }

                        Func<DataRow, bool> funcOneToMany = null;

                        if (!string.IsNullOrWhiteSpace(attr.ChildrenKey) && !string.IsNullOrWhiteSpace(attr.ParentKey))
                        {
                            if (propertyParentKey == null)
                                throw new ArgumentException(string.Format("One to Many relational attributes are required, ParentKey ({0}) property was not found", attr.ParentKey));

                            funcOneToMany = (r) =>
                            {
                                if (r.Table.Columns.Contains(attr.ChildrenKey))
                                    return string.Equals(r[attr.ChildrenKey].ToString(), propertyParentKey.GetValue(parentObj, null).ToString(), StringComparison.OrdinalIgnoreCase);
                                else
                                    throw new ArgumentException(string.Format("One to Many relational attributes are required, ChildrenKey ({0}) column was not found", attr.ChildrenKey));
                            };
                        }
                        else if (attr.ChildrenKeys != null && attr.ChildrenKeys.Any() && propertyParentKeys != null && propertyParentKeys.Any() && !propertyParentKeys.Any(p => p.Value == null)
                            && attr.ChildrenKeys.Count() == propertyParentKeys.Count())
                        {
                            funcOneToMany = (r) =>
                            {
                                bool resul = false;
                                for (int i = 0; i < attr.ChildrenKeys.Length; i++)
                                {
                                    if (propertyParentKeys.TryGetValue(attr.ParentKeys[i], out propertyParentKey))
                                    {
                                        if (r.Table.Columns.Contains(attr.ChildrenKeys[i]))
                                            resul = string.Equals(r[attr.ChildrenKeys[i]].ToString(), propertyParentKey.GetValue(parentObj, null).ToString(), StringComparison.OrdinalIgnoreCase);
                                        else
                                            throw new ArgumentException(string.Format("One to Many relational attributes are required, ChildrenKey ({0}) column was not found", attr.ChildrenKeys[i]));
                                    }
                                    else
                                    {
                                        throw new ArgumentException("One to Many relational attributes must match");
                                    }
                                    if (!resul) break;
                                }
                                return resul;
                            };
                        }
                        else
                        {
                            throw new ArgumentException("One to Many relational attributes are required");
                        }

                        var dataRows = datatableOneToMany.Select().Where(funcOneToMany).ToArray();

                        if (!dataRows.Any())
                            continue;

                        MethodInfo method = typeof(DataTableMap).GetMethod("GetEntity", BindingFlags.Static | BindingFlags.NonPublic);
                        MethodInfo generic = method.MakeGenericMethod(new[] { prop.PropertyType.GetGenericArguments()[0] });

                        Type listGenericType = typeof(List<>);

                        Type list = listGenericType.MakeGenericType(prop.PropertyType.GetGenericArguments()[0]);

                        ConstructorInfo ci = list.GetConstructor(new Type[] { });

                        var listEntities = ci.Invoke(new object[] { });

                        for (int i = 0; i < dataRows.Length; i++)
                        {
                            var obj = generic.Invoke(null, new object[] { dataRows[i], dataTables, convertToPrimitive, parentObj, false });
                            listEntities.GetType().GetMethod("Add").Invoke(listEntities, new[] { obj });
                        }

                        prop.SetValue(parentObj, listEntities, null);
                    }
                }
            }
        }

        private static PropertyInfo SearchPropertyParent(IEnumerable<PropertyInfo> propertiesNames, IEnumerable<PropertyInfo> propertiesNamesObj, string attrParentKey)
        {
            var propertyParentKeys = propertiesNames.Where(p =>
            {
                var a = p.GetCustomAttributes(typeof(ColumnName), true).Single();

                return string.Equals((a as ColumnName).Name, attrParentKey, StringComparison.OrdinalIgnoreCase);
            });

            if(propertyParentKeys.Count() > 1)
                throw new System.InvalidOperationException(string.Format("More than one property contains an attribute ColumnName with the Name:{0}", attrParentKey));

            var propertyParentKey = propertyParentKeys.SingleOrDefault();

            if (propertyParentKey == null)
            {
                propertyParentKey = propertiesNamesObj.Where(p =>
                    string.Equals(p.Name, attrParentKey, StringComparison.OrdinalIgnoreCase)
                ).SingleOrDefault();
            }

            return propertyParentKey;
        }

       }

    /// <summary>
    /// Attribute used to indicate properties that will not be considered during the conversion.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class NotMapped : System.Attribute
    {
    }

    /// <summary>
    /// Attribute used to indicate one to many properties, with their key/s. 
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class OneToManyRelation : System.Attribute
    {
        private string _datatableName;
        private string[] _parentKeys;
        private string[] _childrenKeys;
        private string _parentKey;
        private string _childrenKey;

        public OneToManyRelation(string tableName, string parentKey, string childrenKey)
        {
            _datatableName = tableName;
            _parentKey = parentKey;
            _childrenKey = childrenKey;
        }

        public OneToManyRelation(string tableName, string[] parentKeys, string[] childrenKeys)
        {
            _datatableName = tableName;
            _parentKeys = parentKeys;
            _childrenKeys = childrenKeys;
        }

        public string TableName
        {
            get
            {
                return _datatableName;
            }
        }

        public string[] ParentKeys
        {
            get
            {
                return _parentKeys;
            }
        }

        public string[] ChildrenKeys
        {
            get
            {
                return _childrenKeys;
            }
        }

        public string ParentKey
        {
            get
            {
                return _parentKey;
            }
        }

        public string ChildrenKey
        {
            get
            {
                return _childrenKey;
            }
        }

    }

    /// <summary>
    /// Attribute used to indicate one to one properties, with their key/s. 
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class OneToOneRelation : System.Attribute
    {
        private string _datatableName;
        private string[] _parentKeys;
        private string[] _siblingKeys;
        private string _parentKey;
        private string _siblingKey;
        private bool _required;


        public OneToOneRelation(string tableName, string parentKey, string siblingKey, bool required = false)
        {
            _datatableName = tableName;
            _parentKey = parentKey;
            _siblingKey = siblingKey;
            _required = required;
        }

        public OneToOneRelation(string tableName, string[] parentKeys, string[] siblingKeys, bool required = false)
        {
            _datatableName = tableName;
            _parentKeys = parentKeys;
            _siblingKeys = siblingKeys;
            _required = required;
        }

        public string TableName
        {
            get
            {
                return _datatableName;
            }
        }

        public string[] ParentKeys
        {
            get
            {
                return _parentKeys;
            }
        }

        public string[] SiblingKeys
        {
            get
            {
                return _siblingKeys;
            }
        }

        public string ParentKey
        {
            get
            {
                return _parentKey;
            }
        }

        public string SiblingKey
        {
            get
            {
                return _siblingKey;
            }
        }
        public bool Required
        {
            get
            {
                return _required;
            }
        }


    }

    /// <summary>
    /// Attribute used to indicate the colunm to be use to convert the data to this property.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class ColumnName : System.Attribute
    {
        private string _name;

        public ColumnName(string name)
        {
            _name = name;
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }
    }

    /// <summary>
    /// Attribute used to indicate in a related entity a Navigation to its parent entity
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class ParentNavigation : System.Attribute
    {

    }

    /// <summary>
    /// Convert to primmitive class.
    /// </summary>
    public class ConvertToPrimitive
    {
        public void Convert<T>(PropertyInfo prop, T entity, object value)
        {
            if (prop.PropertyType == typeof(string))
                ConvertString<T>(prop, entity, value);

            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
            {
                ConvertInt<T>(prop, entity, value);
            }
            else if (prop.PropertyType == typeof(long) || prop.PropertyType == typeof(long?))
            {
                ConvertLong<T>(prop, entity, value);
            }
            else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(Nullable<DateTime>))
            {
                ConvertDate<T>(prop, entity, value);
            }
            else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
            {
                ConvertDecimal<T>(prop, entity, value);
            }
            else if (prop.PropertyType == typeof(double) || prop.PropertyType == typeof(double?))
            {
                ConvertDouble<T>(prop, entity, value);
            }
            else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
            {
                ParseBoolean<T>(prop, entity, value);
            }
            else if (prop.PropertyType.BaseType == typeof(System.Enum))
            {
                ParseEnum<T>(prop, entity, value, false);
            }
            else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                && prop.PropertyType.GetGenericArguments().First().BaseType == typeof(System.Enum))
            {
                ParseEnum<T>(prop, entity, value, true);
            }
            else
            {
                var property = prop.PropertyType;
                if (property.IsGenericType && property.GetGenericTypeDefinition() == typeof(Nullable<>))
                    property = property.GetGenericArguments()[0];
                //set property value
                try
                {
                    prop.SetValue(entity, System.Convert.ChangeType(value, property, System.Globalization.CultureInfo.CurrentCulture), null);
                }
                catch(Exception ex)
                {

                }
            }
        }

        public virtual void ConvertString<T>(PropertyInfo prop, T entity, object value)
        {
            if (value != null)
                prop.SetValue(entity, value.ToString().Trim(), null);
            else
                prop.SetValue(entity, null, null);
        }

        public virtual void ConvertInt<T>(PropertyInfo prop, T entity, object value)
        {
            var type = prop.PropertyType;
            int resul;

            if (value != null && !string.IsNullOrEmpty(value.ToString()) && int.TryParse(value.ToString(), out resul))
            {
                prop.SetValue(entity, resul, null);
            }
            else
            {
                prop.SetValue(entity, null, null);
            }
        }

        public virtual void ConvertLong<T>(PropertyInfo prop, T entity, object value)
        {
            var type = prop.PropertyType;
            long resul;

            if (value != null && !string.IsNullOrEmpty(value.ToString()) && long.TryParse(value.ToString(), out resul))
            {
                prop.SetValue(entity, resul, null);
            }
            else
            {
                prop.SetValue(entity, null, null);
            }
        }

        public virtual void ConvertDecimal<T>(PropertyInfo prop, T entity, object value)
        {
            var type = prop.PropertyType;
            decimal resul;

            if (value != null && !string.IsNullOrEmpty(value.ToString()) && decimal.TryParse(value.ToString(), out resul))
            {
                prop.SetValue(entity, resul, null);
            }
            else
            {
                prop.SetValue(entity, null, null);
            }
        }

        public virtual void ConvertDouble<T>(PropertyInfo prop, T entity, object value)
        {
            var type = prop.PropertyType;
            double resul;

            if (value != null && !string.IsNullOrEmpty(value.ToString()) && double.TryParse(value.ToString(), out resul))
            {
                prop.SetValue(entity, resul, null);
            }
            else
            {
                prop.SetValue(entity, null, null);
            }
        }

        public virtual void ConvertDate<T>(PropertyInfo prop, T entity, object value)
        {
            DateTime date;
            bool isValid = DateTime.TryParse(value.ToString(), out date);
            if (isValid)
            {
                prop.SetValue(entity, date, null);
            }
            else
            {
                //Making an assumption here about the format of dates in the source data.
                isValid = DateTime.TryParseExact(value.ToString(), "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.AssumeLocal, out date);
                if (isValid)
                {
                    prop.SetValue(entity, date, null);
                }
            }
        }

        public virtual void ParseBoolean<T>(PropertyInfo prop, T entity, object value)
        {
            var type = prop.PropertyType;
            bool resul;

            if (value != null && !string.IsNullOrEmpty(value.ToString()) && bool.TryParse(value.ToString(), out resul))
            {
                prop.SetValue(entity, resul, null);
            }
            else
            {
                bool res = false;

                if (value == null || value == DBNull.Value)
                    res = false;
                else
                {
                    var val = value.ToString();
                    switch (val.ToLowerInvariant())
                    {
                        case "1":
                        case "y":
                        case "yes":
                        case "true":
                            res = true;
                            break;

                        case "0":
                        case "n":
                        case "no":
                        case "false":
                        default:
                            res = false;
                            break;
                    }
                }
                prop.SetValue(entity, res, null);
            }
        }

        public virtual void ParseEnum<T>(PropertyInfo prop, T entity, object value, bool isNullable)
        {
            if (isNullable)
            {
                var type = prop.PropertyType.GetGenericArguments().First();
                var enumValue = Enum.ToObject(type, Enum.Parse(type, value as string, true));
                prop.SetValue(entity, enumValue, null);
            }
            else
            {
                if (value != null && !string.IsNullOrEmpty(value.ToString()))
                    prop.SetValue(entity, Enum.ToObject(prop.PropertyType, Enum.Parse(prop.PropertyType, value.ToString(), true)), null);
            }
        }

    }

}
