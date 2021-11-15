using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGen
{
    /// <summary>
    /// 用来生成业务层代码
    /// </summary>
    public class BLLGenerator
    {
        private ModelConfig model;
        private string outputPath;

        public BLLGenerator()
        {
            this.model = ModelConfig.GetInstance();
            this.outputPath = Environment.CurrentDirectory + @"\BLL";
        }

        /// <summary>
        /// 生成代码
        /// </summary>
        public void GenerateCode()
        {
            GenerateService();
            GeneratePageService();
        }

        /// <summary>
        /// 生成业务代码
        /// </summary>
        private void GenerateService()
        {
            foreach (Type type in model.POCOs)
            {
                CodeCompileUnit unit = new CodeCompileUnit();
                CodeNamespace nameSpace = new CodeNamespace(model.nameSpace.Split('.')[0] + ".BLL");
                nameSpace.Imports.Add(new CodeNamespaceImport("System"));
                nameSpace.Imports.Add(new CodeNamespaceImport(model.nameSpace));
            }
        }

        /// <summary>
        /// 生成带分页的业务代码
        /// </summary>
        private void GeneratePageService()
        {

        }
    }
}
