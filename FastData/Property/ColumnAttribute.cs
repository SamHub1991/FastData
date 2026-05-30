using System;

namespace FastData.Property
{
    /// <summary>
    /// 字段属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        /// <summary>
        /// 主键
        /// </summary>
        public bool IsKey { get; set; }
        
        /// <summary>
        /// 自增列
        /// </summary>
        public bool IsIdentity { get; set; }
        
        /// <summary>
        /// 长度
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 精度
        /// </summary>
        public int Precision { get; set; }
        
        /// <summary>
        /// 小数点位数
        /// </summary>
        public int Scale { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 是否空
        /// </summary>
        public bool IsNull { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// 列属性构造函数
        /// </summary>
        public ColumnAttribute()
        {
        }

        /// <summary>
        /// 列属性构造函数
        /// </summary>
        /// <param name="isNull">是否允许为空</param>
        /// <param name="comments">字段注释</param>
        public ColumnAttribute(bool isNull = true, string comments = null)
        {
            IsNull = isNull;
            Comments = comments;
        }
    }
}
