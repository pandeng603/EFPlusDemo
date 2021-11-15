using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoGen
{
    /// <summary>
    /// 用来存储实体类
    /// </summary>
    public class ModelConfig
    {
        private static ModelConfig model;
        public List<Type> POCOs { get; set; }
        public string nameSpace { get; set; }
        public Type Context { get; set; }

        private ModelConfig()
        {

        }

        public static ModelConfig GetInstance()
        {
            if (model != null)
            {
                return model;
            } else
            {
                model = new ModelConfig();
                string modelPath = GetModelPath();
                model.nameSpace = modelPath;
                model.POCOs = GetPOCOs(modelPath, model);
                return model;
            }
        }

        /// <summary>
        /// 获取指定命名空间下所有类(排除DbContext类)
        /// </summary>
        /// <param name="modelPath"></param>
        /// <returns></returns>
        private static List<Type> GetPOCOs(string modelPath, ModelConfig model)
        {
            List<Type> types = Assembly.Load(modelPath.Split('.')[0]).GetTypes().Where(t => modelPath.Equals(t.Namespace)).ToList();
            List<Type> results = new List<Type>();
            foreach (Type type in types)
            {
                if (type.BaseType != typeof(DbContext) && !type.Name.Contains("<>c"))//我也不知道为什么要加后面这个判断
                {
                    results.Add(type);
                } else if (type.BaseType == typeof(DbContext)) {
                    model.Context = type;
                }
            }
            return results;
        }

        /// <summary>
        /// 获取目标实体类的命名空间
        /// </summary>
        /// <returns></returns>
        private static string GetModelPath()
        {
            string configPath = Environment.CurrentDirectory + @"\genConfig.json";
            if (!File.Exists(configPath))
            {
                configPath = Environment.CurrentDirectory + @"\Config\genConfig.json";
                if (!File.Exists(configPath))
                {
                    throw new Exception($"在{configPath}找不到配置文件!");
                }
            }
            //读取JSON
            using (var file = File.OpenText(configPath))
            {
                using (var reader = new JsonTextReader(file))
                {
                    JObject o = (JObject)JToken.ReadFrom(reader);
                    string result = o["namespace"]?.ToString();
                    if (string.IsNullOrEmpty(result))
                    {
                        throw new Exception("config文件中没有namespace字段");
                    }
                    return result;
                }
            }
        }
    }
}
