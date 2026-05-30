using System;

namespace FastData.Property
{
    /// <summary>
    /// 主键属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryAttribute : Attribute
    {
        /// <summary>
        /// 主键属性构造函数
        /// </summary>
        public PrimaryAttribute()
        {
        }
    }
}
