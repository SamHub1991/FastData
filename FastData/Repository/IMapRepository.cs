using FastData.Model;
using System.Collections.Generic;

namespace FastData.Repository
{
    /// <summary>
    /// Map 配置仓储接口 - Map 映射相关操作
    /// </summary>
    public interface IMapRepository
    {
        /// <summary>
        /// 获取 Map 数据库
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="isMapDb">是否使用 Map 数据库</param>
        /// <returns>数据库 key</returns>
        string MapDb(string name, bool isMapDb = false);

        /// <summary>
        /// 获取 Map 参数列表
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <returns>参数列表</returns>
        List<string> MapParam(string name);

        /// <summary>
        /// 获取 API 信息
        /// </summary>
        /// <returns>API 字典</returns>
        Dictionary<string, object> Api();

        /// <summary>
        /// 检查 Map 是否存在
        /// </summary>
        /// <param name="xml">XML 文件名</param>
        /// <param name="dbKey">数据库 key</param>
        /// <returns>是否存在</returns>
        bool CheckMap(string xml, string dbKey = null);

        /// <summary>
        /// 获取 Map 类型
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <returns>类型</returns>
        string MapType(string name);

        /// <summary>
        /// 获取 Map 视图
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <returns>视图名称</returns>
        string MapView(string name);

        /// <summary>
        /// 检查 SQL 是否存在
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <returns>是否存在</returns>
        bool IsExists(string name);

        /// <summary>
        /// 检查是否记录日志
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <returns>是否记录日志</returns>
        bool IsMapLog(string name);

        /// <summary>
        /// 获取 Map 备注
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <returns>备注</returns>
        string MapRemark(string name);

        /// <summary>
        /// 获取 Map 参数备注
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数名</param>
        /// <returns>参数备注</returns>
        string MapParamRemark(string name, string param);

        /// <summary>
        /// 获取参数是否必填
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数名</param>
        /// <returns>是否必填</returns>
        string MapRequired(string name, string param);

        /// <summary>
        /// 获取参数最大长度
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数名</param>
        /// <returns>最大长度</returns>
        string MapMaxlength(string name, string param);

        /// <summary>
        /// 获取参数日期格式
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数名</param>
        /// <returns>日期格式</returns>
        string MapDate(string name, string param);

        /// <summary>
        /// 获取参数检查 Map
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数名</param>
        /// <returns>检查 Map</returns>
        string MapCheckMap(string name, string param);

        /// <summary>
        /// 获取参数存在 Map
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数名</param>
        /// <returns>存在 Map</returns>
        string MapExistsMap(string name, string param);

        /// <summary>
        /// 获取数据库配置
        /// </summary>
        /// <param name="name">配置名称</param>
        /// <returns>配置模型</returns>
        ConfigModel DbConfig(string name);

        /// <summary>
        /// 设置数据库 key
        /// </summary>
        /// <param name="key">数据库 key</param>
        /// <returns>仓储实例</returns>
        IFastRepository SetKey(string key);
    }
}
