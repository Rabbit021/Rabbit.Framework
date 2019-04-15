using System;

namespace Rabbit.WebApiFramework.Core.ORM
{
    // 分表策略
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StrategyAttribute : Attribute
    {
        public StrategyAttribute(StrategyCategory category)
        {
            Category = category;
        }
        public StrategyCategory Category { get; set; }
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public class CacheStrategyAttribute : Attribute
    {
        public int Duration { get; set; } = int.MaxValue;
        public CacheStrategyCategory Strategy { get; set; }
        public CacheStrategyAttribute()
        {

        }
    }

    // 默认数据长度
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class DefaultDataTypeAttribute : Attribute
    {
        public DefaultDataTypeAttribute(DbDataType dataType, int length, string defaultVal = "")
        {
            DataType = dataType;
            Length = length;
            DefaultValue = defaultVal;
        }

        public bool IsNullable { get; set; }
        public string DefaultValue { get; set; }
        public DbDataType DataType { get; set; }
        public int Length { get; set; }
    }
}