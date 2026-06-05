using System;
using System.Reflection;
using System.Text.RegularExpressions;
using FastUntility.Attributes;
using System.IO;

#if NET6_0_OR_GREATER
using System.Text.Json;
#else
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace FastUntility.Base
{
    /// <summary>
    /// 常用类型转换和验证工具类
    /// 提供基础类型转换、格式验证等功能
    /// </summary>
    public static class BaseRegular
    {
        #region 序列化与反序列化

        /// <summary>
        /// 将对象序列化为字节数组
        /// </summary>
        /// <param name="value">待序列化的对象</param>
        /// <returns>序列化后的字节数组</returns>
        public static byte[] ToByte(this object value)
        {
#if NET6_0_OR_GREATER
            return JsonSerializer.SerializeToUtf8Bytes(value);
#else
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, value);
                return stream.ToArray();
            }
#endif
        }

        /// <summary>
        /// 将字节数组反序列化为指定类型对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="value">字节数组</param>
        /// <returns>反序列化后的对象</returns>
        public static T ToModel<T>(this byte[] value) where T : class, new()
        {
#if NET6_0_OR_GREATER
            return JsonSerializer.Deserialize<T>(value);
#else
            using (var stream = new MemoryStream(value))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(stream) as T;
            }
#endif
        }

        /// <summary>
        /// 将字节数组反序列化为对象
        /// </summary>
        /// <param name="value">字节数组</param>
        /// <returns>反序列化后的对象</returns>
        public static object ToModel(this byte[] value)
        {
#if NET6_0_OR_GREATER
            return JsonSerializer.Deserialize<object>(value);
#else
            using (var stream = new MemoryStream(value))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(stream);
            }
#endif
        }

        #endregion

        #region 时间转换

        /// <summary>
        /// 转换为可空 DateTime 类型
        /// </summary>
        /// <param name="value">待转换的对象</param>
        /// <returns>转换后的 DateTime，失败时返回 null</returns>
        public static DateTime? ToDate(this object value)
        {
            if (value == null)
                return null;

            var str = value.ToString();
            if (string.IsNullOrEmpty(str))
                return null;

            DateTime result;
            return DateTime.TryParse(str, out result) ? result : (DateTime?)null;
        }

        /// <summary>
        /// 转换为 DateTime 类型
        /// </summary>
        /// <param name="value">待转换的字符串</param>
        /// <returns>转换后的 DateTime，失败时返回 DateTime.MinValue</returns>
        public static DateTime ToDate(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return DateTime.MinValue;

            DateTime result;
            return DateTime.TryParse(value, out result) ? result : DateTime.MinValue;
        }

        /// <summary>
        /// 转换为指定格式的日期字符串
        /// </summary>
        /// <param name="value">待转换的对象</param>
        /// <param name="format">日期格式，如 "yyyy-MM-dd"</param>
        /// <returns>格式化后的日期字符串，失败时返回 null</returns>
        public static string ToDate(this object value, string format)
        {
            if (value == null || string.IsNullOrEmpty(format))
                return null;

            var str = value.ToString();
            if (string.IsNullOrEmpty(str))
                return null;

            DateTime result;
            return DateTime.TryParse(str, out result) ? result.ToString(format) : null;
        }

        /// <summary>
        /// 将 DateTime 转换为指定格式的字符串
        /// </summary>
        /// <param name="value">待转换的 DateTime</param>
        /// <param name="format">日期格式，如 "yyyy-MM-dd"</param>
        /// <returns>格式化后的日期字符串</returns>
        public static string ToDate(this DateTime value, string format)
        {
            if (string.IsNullOrEmpty(format))
                return null;

            return value.ToString(format);
        }

        /// <summary>
        /// 验证字符串是否为有效日期
        /// </summary>
        /// <param name="value">待验证的日期字符串</param>
        /// <returns>是否为有效日期</returns>
        public static bool IsDate(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            DateTime result;
            return DateTime.TryParse(value, out result);
        }

        #endregion

        #region 类型转换

        /// <summary>
        /// 转换为 Int32 类型
        /// </summary>
        /// <param name="value">待转换的字符串</param>
        /// <param name="defaultValue">转换失败时的默认值</param>
        /// <returns>转换后的整数</returns>
        public static int ToInt(this string value, int defaultValue)
        {
            int result;
            return int.TryParse(value, out result) ? result : defaultValue;
        }

        /// <summary>
        /// 转换为 float 类型
        /// </summary>
        /// <param name="value">待转换的字符串</param>
        /// <param name="defaultValue">转换失败时的默认值</param>
        /// <returns>转换后的浮点数</returns>
        public static float ToFloat(this string value, float defaultValue)
        {
            float result;
            return float.TryParse(value, out result) ? result : defaultValue;
        }

        /// <summary>
        /// 转换为 double 类型
        /// </summary>
        /// <param name="value">待转换的字符串</param>
        /// <param name="defaultValue">转换失败时的默认值</param>
        /// <returns>转换后的双精度浮点数</returns>
        public static double ToDouble(this string value, double defaultValue)
        {
            double result;
            return double.TryParse(value, out result) ? result : defaultValue;
        }

        /// <summary>
        /// 转换为 long 类型
        /// </summary>
        /// <param name="value">待转换的字符串</param>
        /// <param name="defaultValue">转换失败时的默认值</param>
        /// <returns>转换后的长整数</returns>
        public static long ToLong(this string value, long defaultValue)
        {
            long result;
            return long.TryParse(value, out result) ? result : defaultValue;
        }

        /// <summary>
        /// 转换为 decimal 类型
        /// </summary>
        /// <param name="value">待转换的字符串</param>
        /// <param name="defaultValue">转换失败时的默认值</param>
        /// <returns>转换后的高精度小数</returns>
        public static decimal ToDecimal(this string value, decimal defaultValue)
        {
            decimal result;
            return decimal.TryParse(value, out result) ? result : defaultValue;
        }

        /// <summary>
        /// 转换为 byte 类型
        /// </summary>
        /// <param name="value">待转换的字符串</param>
        /// <param name="defaultValue">转换失败时的默认值</param>
        /// <returns>转换后的字节</returns>
        public static byte ToByte(this string value, byte defaultValue)
        {
            byte result;
            return byte.TryParse(value, out result) ? result : defaultValue;
        }

        /// <summary>
        /// 转换为 Int16 类型
        /// </summary>
        /// <param name="value">待转换的字符串</param>
        /// <param name="defaultValue">转换失败时的默认值</param>
        /// <returns>转换后的短整数</returns>
        public static short ToInt16(this string value, short defaultValue)
        {
            short result;
            return short.TryParse(value, out result) ? result : defaultValue;
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <param name="value">待转换的对象</param>
        /// <returns>字符串表示，null 时返回空字符串</returns>
        public static string ToStr(this object value)
        {
            return value == null ? string.Empty : value.ToString();
        }

        #endregion

        #region 格式验证

        /// <summary>
        /// 验证固定电话号码
        /// 支持格式：区号-号码，如 010-12345678
        /// </summary>
        /// <param name="value">待验证的电话号码</param>
        /// <returns>是否为有效固定电话号码</returns>
        public static bool IsTelephone(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return Regex.IsMatch(value, @"^(\d{3,4}-)?\d{6,8}$");
        }

        /// <summary>
        /// 验证手机号码
        /// 支持中国大陆主流手机号码格式
        /// </summary>
        /// <param name="value">待验证的手机号码</param>
        /// <returns>是否为有效手机号码</returns>
        public static bool IsMobilePhone(this string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 11)
                return false;

            return Regex.IsMatch(value, @"^(0|86|17951)?(13[0-9]|15[012356789]|17[678]|18[0-9]|14[57])[0-9]{8}");
        }

        /// <summary>
        /// 验证身份证号码（支持 15 位和 18 位）
        /// </summary>
        /// <param name="value">待验证的身份证号</param>
        /// <returns>是否为有效身份证号</returns>
        public static bool IsIDCard(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            if (value.Length == 18)
                return IsIDCard18(value);
            else if (value.Length == 15)
                return IsIDCard15(value);
            else
                return false;
        }

        /// <summary>
        /// 验证 15 位身份证号（一代身份证）
        /// </summary>
        /// <param name="value">待验证的 15 位身份证号</param>
        /// <returns>是否为有效的一代身份证号</returns>
        private static bool IsIDCard15(string value)
        {
            long number;
            if (!long.TryParse(value, out number) || value.Length != 15)
                return false;

            // 验证省份代码
            var validProvinces = "11,12,13,14,15,21,22,23,31,32,33,34,35,36,37,41,42,43,44,45,46,50,51,52,53,54,61,62,63,64,65,71,81,82,91,";
            var provinceCode = value.Substring(0, 2) + ",";
            if (!validProvinces.Contains(provinceCode))
                return false;

            // 验证出生日期
            var birthdate = value.Substring(6, 6).Insert(4, "/").Insert(2, "/");
            DateTime date;
            if (!DateTime.TryParse(birthdate, out date))
                return false;

            return true;
        }

        /// <summary>
        /// 验证 18 位身份证号（二代身份证，GB11643-1999 标准）
        /// </summary>
        /// <param name="value">待验证的 18 位身份证号</param>
        /// <returns>是否为有效的二代身份证号</returns>
        private static bool IsIDCard18(string value)
        {
            long number;
            if (!long.TryParse(value.Substring(0, 17), out number) || value.Substring(0, 17).Length != 17)
                return false;

            if (!long.TryParse(value.Replace('x', '0').Replace('X', '0'), out number))
                return false;

            // 验证省份代码
            var validProvinces = "11,12,13,14,15,21,22,23,31,32,33,34,35,36,37,41,42,43,44,45,46,50,51,52,53,54,61,62,63,64,65,71,81,82,91,";
            var provinceCode = value.Substring(0, 2) + ",";
            if (!validProvinces.Contains(provinceCode))
                return false;

            // 验证出生日期
            var birthdate = value.Substring(6, 8).Insert(6, "/").Insert(4, "/");
            DateTime date;
            if (!DateTime.TryParse(birthdate, out date))
                return false;

            // 校验码验证
            var verifyCodes = new[] { "1", "0", "x", "9", "8", "7", "6", "5", "4", "3", "2" };
            var weights = new[] { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };

            int sum = 0;
            for (var i = 0; i < 17; i++)
            {
                int digit;
                if (int.TryParse(value[i].ToString(), out digit))
                {
                    sum += weights[i] * digit;
                }
            }

            int remainder;
            Math.DivRem(sum, 11, out remainder);

            if (verifyCodes[remainder] != value.Substring(17, 1).ToLower())
                return false;

            return true;
        }

        /// <summary>
        /// 从身份证号提取出生日期
        /// </summary>
        /// <param name="idCard">身份证号码</param>
        /// <returns>出生日期，解析失败时返回 1900-01-01</returns>
        public static DateTime GetIdCardBirthday(this string idCard)
        {
            if (string.IsNullOrEmpty(idCard))
                return new DateTime(1900, 1, 1);

            string birthday;

            if (idCard.Length == 18)
            {
                birthday = string.Format("{0}-{1}-{2}",
                    idCard.Substring(6, 4),
                    idCard.Substring(10, 2),
                    idCard.Substring(12, 2));
            }
            else if (idCard.Length == 15)
            {
                birthday = string.Format("19{0}-{1}-{2}",
                    idCard.Substring(6, 2),
                    idCard.Substring(8, 2),
                    idCard.Substring(10, 2));
            }
            else
            {
                return new DateTime(1900, 1, 1);
            }

            DateTime result;
            return DateTime.TryParse(birthday, out result) ? result : new DateTime(1900, 1, 1);
        }

        /// <summary>
        /// 检测是否包含中文字符
        /// </summary>
        /// <param name="value">待检测的字符串</param>
        /// <param name="defaultValue">空值时的默认返回值</param>
        /// <returns>是否包含中文字符</returns>
        public static bool IsZhString(this string value, bool defaultValue = true)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            try
            {
                return Regex.IsMatch(value, @"[\u4e00-\u9fa5]");
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion

        #region 枚举特性

        /// <summary>
        /// 获取枚举成员的 RemarkAttribute 特性值
        /// </summary>
        /// <param name="item">枚举成员</param>
        /// <returns>特性中的 Remark 值，不存在时返回空字符串</returns>
        public static string ToEnum(this Enum item)
        {
            if (item == null)
                return string.Empty;

            var field = item.GetType().GetField(item.ToString());
            if (field == null)
                return string.Empty;

            var attributes = field.GetCustomAttributes(typeof(RemarkAttribute), false);
            if (attributes.Length > 0)
            {
                var remarkAttr = attributes[0] as RemarkAttribute;
                return remarkAttr != null ? remarkAttr.Remark : string.Empty;
            }

            return string.Empty;
        }

        #endregion

        #region URL 处理

        /// <summary>
        /// 清理 URL 中的特殊字符，使其适合作为文件名或路径
        /// </summary>
        /// <param name="url">待处理的 URL 字符串</param>
        /// <returns>清理后的字符串</returns>
        public static string TransformUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            return url
                .Replace(" ", "-")
                .Replace("<", "-")
                .Replace(">", "-")
                .Replace("*", "-")
                .Replace("?", "-")
                .Replace(",", "")
                .Replace("/", "-")
                .Replace(";", "-")
                .Replace("*/", "-")
                .Replace("&amp", "")
                .Replace("&", "")
                .Replace("\r\n", "-")
                .Replace("+", "-");
        }

        #endregion
    }
}
