using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using SqlSugar;

namespace Rabbit.WebApiFramework.Core.ORM
{
    public abstract class DbContextBase
    {
        public static Dictionary<string, string> TableMapper = new Dictionary<string, string>();
        public static Dictionary<string, StrategyAttribute> TableStrategy = new Dictionary<string, StrategyAttribute>();
        public static Dictionary<string, CacheStrategyAttribute> TableCacheStrategy = new Dictionary<string, CacheStrategyAttribute>();

        public ICacheService CacheService;
        public SqlSugarClient Db;
        protected int Duration = int.MaxValue;
        protected bool UseCache;
        protected DbContextBase()
        {
        }
        protected DbContextBase(SqlSugarClient db)
        {
            Db = db;
        }
        public IDbMaintenance DbMaintenance => Db.DbMaintenance;
        public EntityMaintenance EntityMaintenance => Db.EntityMaintenance;
        protected string BuildingSign { get; set; }
        protected string ConnectionString { get; set; }
        protected DbType DbType { get; set; }
        protected void CreateDb()
        {
            try
            {
                var config = new ConnectionConfig();
                config.ConnectionString = ConnectionString;
                config.DbType = DbType;
                config.IsAutoCloseConnection = true;
                config.IsShardSameThread = true;
                config.InitKeyType = InitKeyType.Attribute;
                LoggerManager.Instance.Info(config.ConnectionString);

                Db = new SqlSugarClient(config);
                Db.Ado.CommandTimeOut = 3000;
                Db.Ado.IsEnableLogEvent = true;
                Db.Ado.LogEventStarting = (sql, pars) =>
                {
                    Console.WriteLine(sql + "\r\n" + Db.Utilities.SerializeObject(pars));
                    Console.WriteLine();
                };
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected DbSet<TSource> GetDbSet<TSource>() where TSource : class, new()
        {
            return new DbSet<TSource>(Db);
        }

        public bool IsAny<TSource>(Expression<Func<TSource, bool>> whereExpression) where TSource : class, new()
        {
            return GetDbSet<TSource>().IsAny(whereExpression);
        }

        public int Count<TSource>(Expression<Func<TSource, bool>> whereExpression) where TSource : class, new()
        {
            return GetDbSet<TSource>().Count(whereExpression);
        }

        public bool UseTran(Action action)
        {
            var rst = Db.Ado.UseTran(action);
            return rst.IsSuccess;
        }

        #region 数据库维护

        public bool IsAnyTable(string tableName, bool isCache = true)
        {
            return DbMaintenance.IsAnyTable(tableName, isCache);
        }

        #endregion

        #region 获取表名和缓存策略

        public virtual string GetTableName<TSource>() where TSource : new()
        {
            return GetTableName(typeof(TSource));
        }

        public virtual string GetTableName<TSource>(string tableName) where TSource : new()
        {
            if (string.IsNullOrEmpty(tableName))
                tableName = GetTableName<TSource>();
            return tableName;
        }

        public virtual string GetTableName(Type type)
        {
            var tableName = type.Name;
            StrategyAttribute strategy = null;
            var key = type.FullName;
            try
            {
                if (!TableStrategy.TryGetValue(key, out strategy))
                {
                    strategy = type.GetCustomAttribute<StrategyAttribute>(false);
                    TableStrategy.Add(key, strategy);
                }

                if (!TableMapper.TryGetValue(key, out tableName))
                {
                    var tableAttr = type.GetCustomAttribute<SugarTable>(false);
                    tableName = string.IsNullOrEmpty(tableAttr?.TableName) ? type.Name : tableAttr?.TableName;
                    TableMapper.Add(key, tableName);
                }

                if (strategy != null)
                    switch (strategy.Category)
                    {
                        case StrategyCategory.Building:
                            tableName = $"{tableName}_{BuildingSign}";
                            break;
                    }
                return tableName;
            }
            catch
            {
                Console.WriteLine("[实体Key重复]" + key);
                return tableName;
            }
        }
        public virtual CacheStrategyAttribute GetCacheStrategy(Type type)
        {
            var key = type.FullName + "";
            if (!TableCacheStrategy.TryGetValue(key, out var strategy))
            {
                strategy = type.GetCustomAttribute<CacheStrategyAttribute>();
                TableCacheStrategy.Add(key, strategy);
            }

            return strategy;
        }
        #endregion

        #region ISugarQueryable
        public ISugarQueryable<TSource> Queryable<TSource>(string table = "") where TSource : class, new()
        {
            if (string.IsNullOrEmpty(table))
                table = GetTableName<TSource>();
            var rst = Db.Queryable<TSource>().AS(table);
            if (UseCache)
            {
                var strategy = GetCacheStrategy(typeof(TSource));
                if (strategy != null)
                    switch (strategy.Strategy)
                    {
                        case CacheStrategyCategory.None:
                            rst.WithCache(strategy.Duration);
                            break;
                    }
            }

            return rst;
        }

        public ISugarQueryable<TSource> Queryable<TSource>(Expression<Func<TSource, bool>> whereExpression) where TSource : class, new()
        {
            return Queryable<TSource>().Where(whereExpression);
        }

        public ISugarQueryable<TSource> Queryable<TSource>(Expression<Func<TSource, bool>> whereExpression, string tablename) where TSource : class, new()
        {
            return Queryable<TSource>(tablename).Where(whereExpression);
        }

        #endregion

        #region  实例共享

        public static readonly ConcurrentDictionary<string, DbContextBase> _cache = new ConcurrentDictionary<string, DbContextBase>();
        public static readonly ThreadLocal<string> _threadLocal;

        static DbContextBase()
        {
            _threadLocal = new ThreadLocal<string>();
        }

        public static TSource Create<TSource>() where TSource : DbContextBase, new()
        {
            return new TSource();
            // TODO 放弃使用线程缓存了
            _cache.TryGetValue(_threadLocal.Value + "", out var context);
            var rst = context as TSource;
            if (rst == null)
            {
                var key = ShortGuid.NewGuid();
                context = new TSource();
                _cache.TryAdd(key, context);
                _threadLocal.Value = key;
            }

            return context as TSource;
        }

        public static TSource Create<TSource>(string buildingSign) where TSource : DbContextBase, new()
        {
            var context = new TSource();
            context.BuildingSign = buildingSign;
            return context;
        }

        public static void Release()
        {
            try
            {
                if (_cache.Count > 100)
                {
                    var id = _threadLocal.Value + "";
                    if (!_cache.ContainsKey(id))
                        return;
                    Remove(id);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private static bool Remove(string id)
        {
            if (!_cache.ContainsKey(id)) return false;
            var index = 0;
            var result = false;
            while (!(result = _cache.TryRemove(id, out var client)))
            {
                index++;
                Thread.Sleep(20);
                if (index > 3) break;
            }

            return result;
        }

        #endregion

        #region Select

        public List<TSource> GetList<TSource>() where TSource : class, new()
        {
            return Queryable<TSource>().ToList();
        }

        public List<TSource> GetList<TSource>(string tablename) where TSource : class, new()
        {
            return Queryable<TSource>(tablename).ToList();
        }

        public List<TSource> GetList<TSource>(Expression<Func<TSource, bool>> whereExpression) where TSource : class, new()
        {
            return Queryable(whereExpression).ToList();
        }

        public List<TSource> GetList<TSource>(Expression<Func<TSource, bool>> whereExpression, string tablename) where TSource : class, new()
        {
            return Queryable(whereExpression, tablename).ToList();
        }

        public List<TSource> GetDescOrderList<TSource>(Expression<Func<TSource, bool>> whereExpression, Expression<Func<TSource, object>> orderexp) where TSource : class, new()
        {
            return Queryable(whereExpression).OrderBy(orderexp, OrderByType.Desc).ToList();
        }

        public List<TSource> GetAescOrderList<TSource>(Expression<Func<TSource, bool>> whereExpression, Expression<Func<TSource, object>> orderexp) where TSource : class, new()
        {
            return Queryable(whereExpression).OrderBy(orderexp).ToList();
        }

        public TSource First<TSource>() where TSource : class, new()
        {
            return Queryable<TSource>().First();
        }

        public TSource First<TSource>(Expression<Func<TSource, bool>> whereExpression) where TSource : class, new()
        {
            return Queryable<TSource>().First(whereExpression);
        }

        public TSource First<TSource>(Expression<Func<TSource, bool>> whereExpression, string tablename) where TSource : class, new()
        {
            return Queryable<TSource>(tablename).First(whereExpression);
        }

        public List<TSource> GetPageList<TSource>(Expression<Func<TSource, bool>> whereExpression, PageModel page) where TSource : class, new()
        {
            return GetDbSet<TSource>().GetPageList(whereExpression, page);
        }

        public List<TSource> GetPageList<TSource>(Expression<Func<TSource, bool>> whereExpression, PageModel page, Expression<Func<TSource, object>> orderByExpression, OrderByType orderByType = OrderByType.Asc) where TSource : class, new()
        {
            return GetDbSet<TSource>().GetPageList(whereExpression, page, orderByExpression, orderByType);
        }

        public List<TSource> GetPageList<TSource>(List<IConditionalModel> conditionalList, PageModel page) where TSource : class, new()
        {
            return GetDbSet<TSource>().GetPageList(conditionalList, page);
        }

        public List<TSource> GetPageList<TSource>(List<IConditionalModel> conditionalList, PageModel page, Expression<Func<TSource, object>> orderByExpression, OrderByType orderByType = OrderByType.Asc) where TSource : class, new()
        {
            return GetDbSet<TSource>().GetPageList(conditionalList, page, orderByExpression, orderByType);
        }

        #endregion

        #region Insert

        public IInsertable<TSource> Insertable<TSource>(TSource insertObj, string tableName = "") where TSource : class, new()
        {
            return Insertable(new[] { insertObj }, tableName);
        }

        public IInsertable<TSource> Insertable<TSource>(List<TSource> insertObj, string tableName = "") where TSource : class, new()
        {
            return Insertable(insertObj.ToArray(), tableName);
        }

        public IInsertable<TSource> Insertable<TSource>(TSource[] insertObjs, string tableName = "") where TSource : class, new()
        {
            tableName = GetTableName<TSource>(tableName);
            return Db.Insertable(insertObjs).AS(tableName);
        }

        public bool Insert<TSource>(TSource insertObj, string tableName = "") where TSource : class, new()
        {
            if (insertObj == null) return true;
            return Insertable(insertObj, tableName).ExecuteCommand() > 0;
        }

        public int Insert<TSource>(List<TSource> insertObjs, string tableName = "") where TSource : class, new()
        {
            if (insertObjs == null || !insertObjs.Any()) return 0;
            return Insertable(insertObjs, tableName).ExecuteCommand();
        }

        public bool InsertOrUpdate<TSource>(TSource insetObj) where TSource : class, new()
        {
            return Insert(insetObj) || Update(insetObj);
        }

        public bool InsertOrUpdate<TSource>(TSource insetObj, string tableName) where TSource : class, new()
        {
            return Insert(insetObj, tableName) || Update(insetObj, tableName);
        }

        #endregion

        #region Update

        public IUpdateable<TSource> Updateable<TSource>(string tableName = "") where TSource : class, new()
        {
            tableName = GetTableName<TSource>(tableName);
            return Db.Updateable<TSource>().AS(tableName);
        }

        public IUpdateable<TSource> Updateable<TSource>(TSource[] updateObj, string tableName = "") where TSource : class, new()
        {
            tableName = GetTableName<TSource>(tableName);
            return Db.Updateable(updateObj).AS(tableName);
        }

        public IUpdateable<TSource> Updateable<TSource>(TSource updateObj, string tableName = "") where TSource : class, new()
        {
            return Updateable(new[] { updateObj }).AS(tableName);
        }

        public IUpdateable<TSource> Updateable<TSource>(List<TSource> updateObj, string tableName = "") where TSource : class, new()
        {
            tableName = GetTableName<TSource>(tableName);
            return Updateable(updateObj.ToArray()).AS(tableName);
        }

        public bool Update<TSource>(TSource updateObj, string tablename = "") where TSource : class, new()
        {
            if (updateObj == null) return true;
            return Updateable(updateObj, tablename).ExecuteCommand() > 0;
        }

        public int Update<TSource>(List<TSource> updateObjs, string tablename = "") where TSource : class, new()
        {
            if (updateObjs == null || !updateObjs.Any()) return 0;
            var rst = Updateable(updateObjs, tablename).ExecuteCommand();
            return rst;
        }

        public int Update<TSource>(Expression<Func<TSource, TSource>> columns, Expression<Func<TSource, bool>> whereExpression, string tableName = "") where TSource : class, new()
        {
            var rst = Updateable<TSource>(tableName).UpdateColumns(columns).Where(whereExpression).ExecuteCommand();
            return rst;
        }

        #endregion

        #region Delete

        public IDeleteable<TSource> Deleteable<TSource>(string tableName = "") where TSource : class, new()
        {
            tableName = GetTableName<TSource>(tableName);
            return Db.Deleteable<TSource>().AS(tableName);
        }

        public IDeleteable<TSource> Deleteable<TSource>(TSource deleteobj, string tableName = "") where TSource : class, new()
        {
            tableName = GetTableName<TSource>(tableName);
            return Db.Deleteable(deleteobj).AS(tableName);
        }

        public IDeleteable<TSource> Deleteable<TSource>(List<TSource> deleteobj, string tableName = "") where TSource : class, new()
        {
            tableName = GetTableName<TSource>(tableName);
            return Db.Deleteable(deleteobj).AS(tableName);
        }

        public bool Delete<TSource>(TSource deleteObj, string tablename = "") where TSource : class, new()
        {
            return Deleteable(deleteObj, tablename).ExecuteCommand() > 0;
        }

        public bool Delete<TSource>(Expression<Func<TSource, bool>> whereExpression, string tablename = "") where TSource : class, new()
        {
            tablename = GetTableName<TSource>(tablename);
            return Db.Deleteable<TSource>().AS(tablename).Where(whereExpression).ExecuteCommand() > 0;
        }

        #endregion
    }

    public class DbSet<TSource> : SimpleClient<TSource> where TSource : class, new()
    {
        public DbSet(SqlSugarClient context) : base(context)
        {
        }
    }
}