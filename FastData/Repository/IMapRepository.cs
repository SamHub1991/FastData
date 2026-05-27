using FastData.Model;
using System.Collections.Generic;

namespace FastData.Repository
{
    /// <summary>
    /// Map 配置仓储接口 - Map 映射相关操作
    /// </summary>
    public interface IMapRepository
    {
        string MapDb(string name, bool isMapDb = false);

        List<string> MapParam(string name);

        Dictionary<string, object> Api();

        bool CheckMap(string xml, string dbKey = null);

        string MapType(string name);

        string MapView(string name);

        bool IsExists(string name);

        bool IsMapLog(string name);

        string MapRemark(string name);

        string MapParamRemark(string name, string param);

        string MapRequired(string name, string param);

        string MapMaxlength(string name, string param);

        string MapDate(string name, string param);

        string MapCheckMap(string name, string param);

        string MapExistsMap(string name, string param);

        ConfigModel DbConfig(string name);

        IFastRepository SetKey(string key);
    }
}
