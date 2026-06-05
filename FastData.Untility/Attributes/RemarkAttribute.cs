using System;

namespace FastUntility.Attributes
{
    /// <summary>
    /// 自定义备注特性
    /// 用于为类、方法、属性等添加中文备注说明，可通过运行时反射获取元数据描述
    /// </summary>
    public sealed class RemarkAttribute : Attribute
    {
        private string m_value = "";

        /// <summary>
        /// 初始化备注特性
        /// </summary>
        /// <param name="value">备注内容</param>
        public RemarkAttribute(string value)
        {
            m_value = value;
        }

        /// <summary>
        /// 特性备注
        /// </summary>
        public string Remark
        {
            get { return m_value; }
        }
    }
}
