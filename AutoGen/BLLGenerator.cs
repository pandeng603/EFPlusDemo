using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
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

        private const string threeTabs = "            ";
        private const string fourTabs = "                ";
        private const string fiveTabs = "                    ";

        public BLLGenerator()
        {
            this.model = ModelConfig.GetInstance();
            this.outputPath = Environment.CurrentDirectory + @"\BLL\";
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
                //type为每一个POCO类
                //创建必要对象
                CodeCompileUnit unit = new CodeCompileUnit();
                CodeNamespace nameSpace = new CodeNamespace(model.nameSpace.Split('.')[0] + ".BLL");
                nameSpace.Imports.Add(new CodeNamespaceImport("System"));
                nameSpace.Imports.Add(new CodeNamespaceImport(model.nameSpace));
                CodeTypeDeclaration service = new CodeTypeDeclaration(type.Name + "Service");
                service.IsClass = true;
                service.TypeAttributes = TypeAttributes.Public;
                nameSpace.Types.Add(service);
                unit.Namespaces.Add(nameSpace);
                //添加context
                CodeMemberField context = new CodeMemberField(model.Context.Name, "context");
                context.Attributes = MemberAttributes.Private;
                service.Members.Add(context);
                //找到context中对应type的DbSet属性
                string dbSet = model.Context.GetProperties().Where(p => p.PropertyType.GenericTypeArguments[0].Name.Contains(type.Name)).FirstOrDefault().Name;
                //创建构造方法
                CodeConstructor constructor = new CodeConstructor();
                constructor.Attributes = MemberAttributes.Public;
                constructor.Statements.Add(new CodeSnippetExpression($"this.context = new {model.Context.Name}()"));
                service.Members.Add(constructor);
                //判断该类是否存在主键
                PropertyInfo key = type.GetProperties().Where(p => Attribute.IsDefined(p, typeof(KeyAttribute))).FirstOrDefault();
                if (key != null)
                {
                    //包含主键时，生成以下代码的方法
                    #region 通过主键查询的方法
                    CodeMemberMethod selectById = new CodeMemberMethod();
                    selectById.Name = "Select" + type.Name + "By" + key.Name;
                    selectById.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    selectById.Parameters.Add(new CodeParameterDeclarationExpression(key.PropertyType, "id"));
                    selectById.ReturnType = new CodeTypeReference($"{type.Name}");
                    //方法体
                    //User = context.Users.Where(u => u.UId.Equals(id)).First();
                    //return user;
                    selectById.Statements.Add(new CodeSnippetStatement(threeTabs + $"{type.Name} {type.Name.ToLower()} = context.{dbSet}.Where(e => e.{key.Name}.Equals(id)).First();"));
                    selectById.Statements.Add(new CodeSnippetStatement(threeTabs + $"return {type.Name.ToLower()};"));
                    //加入到类中
                    service.Members.Add(selectById);
                    #endregion
                    #region 通过主键删除的方法
                    CodeMemberMethod removeById = new CodeMemberMethod();
                    removeById.Name = "Remove" + type.Name + "By" + key.Name;
                    removeById.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    removeById.Parameters.Add(new CodeParameterDeclarationExpression(key.PropertyType, "id"));
                    removeById.ReturnType = new CodeTypeReference(typeof(bool));
                    //方法体
                    //User user = context.Users.Where(u => u.UId.Equals(id)).First();
                    //try
                    //{
                    //    context.Users.Remove(user);
                    //    context.SaveChanges();
                    //    return true;
                    //}
                    //catch (Exception)
                    //{
                    //    return false;
                    //}
                    removeById.Statements.Add(new CodeSnippetStatement(threeTabs + $"{type.Name} {type.Name.ToLower()} = context.{dbSet}.Where(e => e.{key.Name}.Equals(id)).First();"));
                    removeById.Statements.Add(new CodeSnippetStatement(threeTabs + "try"));
                    removeById.Statements.Add(new CodeSnippetStatement(threeTabs + "{"));
                    removeById.Statements.Add(new CodeSnippetStatement(fourTabs + $"context.{dbSet}.Remove({type.Name.ToLower()});"));
                    removeById.Statements.Add(new CodeSnippetStatement(fourTabs + "context.SaveChanges();"));
                    removeById.Statements.Add(new CodeSnippetStatement(fourTabs + "return true;"));
                    removeById.Statements.Add(new CodeSnippetStatement(threeTabs + "}"));
                    removeById.Statements.Add(new CodeSnippetStatement(threeTabs + "catch (Exception)"));
                    removeById.Statements.Add(new CodeSnippetStatement(threeTabs + "{"));
                    removeById.Statements.Add(new CodeSnippetStatement(fourTabs + "return false;"));
                    removeById.Statements.Add(new CodeSnippetStatement(threeTabs + "}"));
                    service.Members.Add(removeById);
                    #endregion
                    #region 通过主键修改的方法
                    //User target = context.Users.Where(u => u.UId.Equals(id)).First();
                    //try
                    //{
                    //    target.Age = user.Age;
                    //    ...
                    //    context.SaveChanges();
                    //    return true;
                    //}
                    //catch (Exception)
                    //{
                    //    return false;
                    //}
                    CodeMemberMethod updateById = new CodeMemberMethod();
                    updateById.Name = "Update" + type.Name + "By" + key.Name;
                    updateById.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    updateById.Parameters.Add(new CodeParameterDeclarationExpression(type.Name, type.Name.ToLower()));
                    updateById.ReturnType = new CodeTypeReference(typeof(bool));
                    //方法体
                    updateById.Statements.Add(new CodeSnippetStatement(threeTabs + $"{type.Name} target = context.{dbSet}.Where(e => e.{key.Name}.Equals({type.Name.ToLower()}.{key.Name}).First();"));
                    updateById.Statements.Add(new CodeSnippetStatement(threeTabs + "try"));
                    updateById.Statements.Add(new CodeSnippetStatement(threeTabs + "{"));
                    //循环生成赋值语句
                    foreach (var prop in type.GetProperties())
                    {
                        //判断该属性，筛去导航属性和主键
                        //首先判断主键
                        if (Attribute.IsDefined(prop, typeof(KeyAttribute)))
                        {
                            continue;
                        }
                        //判断是不是导航属性
                        //1. 按照virtual关键字判断
                        if (prop.GetMethod.IsVirtual)
                        {
                            continue;
                        }
                        //2. 按照是否为ICollection类判断
                        string propType = prop.PropertyType.Name;
                        if (propType.Contains("ICollection"))
                        {
                            continue;
                        }
                        //筛选完毕，开始生成代码
                        updateById.Statements.Add(new CodeSnippetStatement(fourTabs + $"target.{prop.Name} = {type.Name.ToLower()}.{prop.Name};"));
                    }
                    updateById.Statements.Add(new CodeSnippetStatement(fourTabs + "context.SaveChanges();"));
                    updateById.Statements.Add(new CodeSnippetStatement(fourTabs + "return true;"));
                    updateById.Statements.Add(new CodeSnippetStatement(threeTabs + "}"));
                    updateById.Statements.Add(new CodeSnippetStatement(threeTabs + "catch (Exception)"));
                    updateById.Statements.Add(new CodeSnippetStatement(threeTabs + "{"));
                    updateById.Statements.Add(new CodeSnippetStatement(fourTabs + "return false;"));
                    updateById.Statements.Add(new CodeSnippetStatement(threeTabs + "}"));
                    service.Members.Add(updateById);
                    #endregion
                }
                //其他常规方法
                #region 获取所有记录
                //List<User> list = context.Users.ToList();
                CodeMemberMethod selectAll = new CodeMemberMethod();
                selectAll.Name = "SelectAll" + type.Name;
                selectAll.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                selectAll.ReturnType = new CodeTypeReference($"List<{type.Name}>");
                //方法体
                selectAll.Statements.Add(new CodeSnippetStatement(threeTabs + $"List<{type.Name}> list = context.{dbSet}.ToList();"));
                selectAll.Statements.Add(new CodeSnippetStatement(threeTabs + "return list;"));

                service.Members.Add(selectAll);
                #endregion
                #region 根据筛选条件查询
                CodeMemberMethod selectByCondition = new CodeMemberMethod();
                selectByCondition.Name = "Select" + type.Name + "ByCondition";
                selectByCondition.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                selectByCondition.Parameters.Add(new CodeParameterDeclarationExpression(type.Name, type.Name.ToLower()));
                selectByCondition.Comments.Add(new CodeCommentStatement("将筛选条件封装为POCO对象传入，默认直接判断所有非主键、非导航属性的字段，请根据需要修改一些判断", true));
                selectByCondition.ReturnType = new CodeTypeReference($"List<{type.Name}>");
                //方法体
                selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + $"var {dbSet.ToLower()} = context.{dbSet};"));
                //循环生成判断语句
                foreach (var prop in type.GetProperties())
                {
                    //判断该属性，筛去导航属性和主键
                    //首先判断主键
                    if (Attribute.IsDefined(prop, typeof(KeyAttribute)))
                    {
                        continue;
                    }
                    //判断是不是导航属性
                    //1. 按照virtual关键字判断
                    if (prop.GetMethod.IsVirtual)
                    {
                        continue;
                    }
                    //2. 按照是否为ICollection类判断
                    string propType = prop.PropertyType.Name;
                    if (propType.Contains("ICollection"))
                    {
                        continue;
                    }
                    //筛选完毕，开始生成代码
                    //如果prop为string类型则：
                    if (prop.PropertyType.Name.Contains("String"))
                    {
                        selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + $"if (!string.IsNullOrEmpty({type.Name.ToLower()}.{prop.Name}))"));
                        selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + "{"));
                        selectByCondition.Statements.Add(new CodeSnippetStatement(fourTabs + $"{dbSet.ToLower()} = {dbSet.ToLower()}.Where(e => e.{prop.Name}.Equals({type.Name.ToLower()}.{prop.Name}))"));
                        selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + "}"));
                    } else
                    {
                        selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + $"{dbSet.ToLower()} = {dbSet.ToLower()}.Where(e => e.{prop.Name}.Equals({type.Name.ToLower()}.{prop.Name}))"));
                    }
                }
                selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + $"return {dbSet.ToLower()}.ToList();"));
                service.Members.Add(selectByCondition);
                #endregion
                #region 添加记录
                CodeMemberMethod addMethod = new CodeMemberMethod();
                addMethod.Name = "Add" + type.Name;
                addMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                addMethod.Parameters.Add(new CodeParameterDeclarationExpression(type.Name, type.Name.ToLower()));
                addMethod.ReturnType = new CodeTypeReference(typeof(bool));
                //方法体
                addMethod.Statements.Add(new CodeSnippetStatement(threeTabs + $"context.{dbSet}.Add({type.Name.ToLower()});"));
                addMethod.Statements.Add(new CodeSnippetStatement(threeTabs + "try"));
                addMethod.Statements.Add(new CodeSnippetStatement(threeTabs + "{"));
                addMethod.Statements.Add(new CodeSnippetStatement(fourTabs + "context.SaveChanges();"));
                addMethod.Statements.Add(new CodeSnippetStatement(fourTabs + "return true;"));
                addMethod.Statements.Add(new CodeSnippetStatement(threeTabs + "}"));
                addMethod.Statements.Add(new CodeSnippetStatement(threeTabs + "catch (Exception)"));
                addMethod.Statements.Add(new CodeSnippetStatement(threeTabs + "{"));
                addMethod.Statements.Add(new CodeSnippetStatement(fourTabs + "return false;"));
                addMethod.Statements.Add(new CodeSnippetStatement(threeTabs + "}"));
                service.Members.Add(addMethod);
                #endregion
                #region 添加记录集合
                CodeMemberMethod addRangeMethod = new CodeMemberMethod();
                addRangeMethod.Name = "Add" + type.Name + "Range";
                addRangeMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                addRangeMethod.Parameters.Add(new CodeParameterDeclarationExpression("List<" + type.Name + ">", dbSet.ToLower()));
                addRangeMethod.ReturnType = new CodeTypeReference(typeof(bool));
                //方法体
                addRangeMethod.Statements.Add(new CodeSnippetStatement(threeTabs + $"context.{dbSet}.AddRange({dbSet.ToLower()});"));
                addRangeMethod.Statements.Add(new CodeSnippetStatement(threeTabs + "try"));
                addRangeMethod.Statements.Add(new CodeSnippetStatement(threeTabs + "{"));
                addRangeMethod.Statements.Add(new CodeSnippetStatement(fourTabs + "context.SaveChanges();"));
                addRangeMethod.Statements.Add(new CodeSnippetStatement(fourTabs + "return true;"));
                addRangeMethod.Statements.Add(new CodeSnippetStatement(threeTabs + "}"));
                addRangeMethod.Statements.Add(new CodeSnippetStatement(threeTabs + "catch (Exception)"));
                addRangeMethod.Statements.Add(new CodeSnippetStatement(threeTabs + "{"));
                addRangeMethod.Statements.Add(new CodeSnippetStatement(fourTabs + "return false;"));
                addRangeMethod.Statements.Add(new CodeSnippetStatement(threeTabs + "}"));
                service.Members.Add(addRangeMethod);
                #endregion
                //写到文件
                CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
                CodeGeneratorOptions options = new CodeGeneratorOptions();
                options.BracingStyle = "C";
                options.BlankLinesBetweenMembers = true;
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }
                using (var sw = new StreamWriter(outputPath + type.Name + "Service.cs", false))
                {
                    provider.GenerateCodeFromCompileUnit(unit, sw, options);
                }
            }
        }

        /// <summary>
        /// 生成带分页的业务代码
        /// </summary>
        private void GeneratePageService()
        {
            foreach (Type type in model.POCOs)
            {
                //type为每一个POCO类
                //创建必要对象
                CodeCompileUnit unit = new CodeCompileUnit();
                CodeNamespace nameSpace = new CodeNamespace(model.nameSpace.Split('.')[0] + ".BLL");
                nameSpace.Imports.Add(new CodeNamespaceImport("System"));
                nameSpace.Imports.Add(new CodeNamespaceImport(model.nameSpace));
                CodeTypeDeclaration service = new CodeTypeDeclaration(type.Name + "PageService");
                service.IsClass = true;
                service.TypeAttributes = TypeAttributes.Public;
                nameSpace.Types.Add(service);
                unit.Namespaces.Add(nameSpace);
                //添加context
                CodeMemberField context = new CodeMemberField(model.Context.Name, "context");
                context.Attributes = MemberAttributes.Private;
                service.Members.Add(context);
                //找到context中对应type的DbSet属性
                string dbSet = model.Context.GetProperties().Where(p => p.PropertyType.GenericTypeArguments[0].Name.Contains(type.Name)).FirstOrDefault().Name;
                //创建构造方法
                CodeConstructor constructor = new CodeConstructor();
                constructor.Attributes = MemberAttributes.Public;
                constructor.Statements.Add(new CodeSnippetExpression($"this.context = new {model.Context.Name}()"));
                service.Members.Add(constructor);

                #region 直接分页查询数据（不筛选）
                //参数：目标页码pageNum，每页记录数pageSize
                CodeMemberMethod selectAll = new CodeMemberMethod();
                selectAll.Name = "SelectAll" + type.Name;
                selectAll.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                selectAll.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "pageNum"));
                selectAll.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "pageSize"));
                selectAll.ReturnType = new CodeTypeReference($"List<{type.Name}>");
                //方法体
                //List<Category> list = context.Categories.Skip((pageNum - 1) * pageSize).Take(pageSize).ToList();
                selectAll.Statements.Add(new CodeSnippetStatement(threeTabs + $"List<{type.Name}> list = context.{dbSet}.Skip((pageNum - 1) * pageSize).Take(pageSize).ToList();"));
                selectAll.Statements.Add(new CodeSnippetStatement(threeTabs + "return list;"));
                service.Members.Add(selectAll);
                #endregion

                #region 筛选分页查询
                //参数：目标页码pageNum，每页记录数pageSize，用于筛选的实体对象type.Name.ToLower()
                CodeMemberMethod selectByCondition = new CodeMemberMethod();
                selectByCondition.Name = "Select" + type.Name + "ByCondition";
                selectByCondition.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                selectByCondition.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "pageNum"));
                selectByCondition.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "pageSize"));
                selectByCondition.Parameters.Add(new CodeParameterDeclarationExpression(type.Name, type.Name.ToLower()));
                selectByCondition.ReturnType = new CodeTypeReference($"List<{type.Name}>");
                selectByCondition.Comments.Add(new CodeCommentStatement("将筛选条件封装为POCO对象传入，默认直接判断所有非主键、非导航属性的字段，请根据需要修改一些判断", true));
                //方法体
                selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + $"var {dbSet.ToLower()} = context.{dbSet};"));
                //循环生成判断语句
                foreach (var prop in type.GetProperties())
                {
                    //判断该属性，筛去导航属性和主键
                    //首先判断主键
                    if (Attribute.IsDefined(prop, typeof(KeyAttribute)))
                    {
                        continue;
                    }
                    //判断是不是导航属性
                    //1. 按照virtual关键字判断
                    if (prop.GetMethod.IsVirtual)
                    {
                        continue;
                    }
                    //2. 按照是否为ICollection类判断
                    string propType = prop.PropertyType.Name;
                    if (propType.Contains("ICollection"))
                    {
                        continue;
                    }
                    //筛选完毕，开始生成代码
                    //如果prop为string类型则：
                    if (prop.PropertyType.Name.Contains("String"))
                    {
                        selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + $"if (!string.IsNullOrEmpty({type.Name.ToLower()}.{prop.Name}))"));
                        selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + "{"));
                        selectByCondition.Statements.Add(new CodeSnippetStatement(fourTabs + $"{dbSet.ToLower()} = {dbSet.ToLower()}.Where(e => e.{prop.Name}.Equals({type.Name.ToLower()}.{prop.Name}))"));
                        selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + "}"));
                    }
                    else
                    {
                        selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + $"{dbSet.ToLower()} = {dbSet.ToLower()}.Where(e => e.{prop.Name}.Equals({type.Name.ToLower()}.{prop.Name}))"));
                    }
                }
                //将筛选完的数据集分页
                selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + $"List<{type.Name}> list = {dbSet.ToLower()}.Skip((pageNum - 1) * pageSize).Take(pageSize).ToList();"));
                selectByCondition.Statements.Add(new CodeSnippetStatement(threeTabs + "return list;"));
                service.Members.Add(selectByCondition);
                #endregion

                #region 获取最大页数
                //参数：每页记录数pageSize
                CodeMemberMethod getTotalPageNum = new CodeMemberMethod();
                getTotalPageNum.Name = "GetTotalPageNum";
                getTotalPageNum.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                getTotalPageNum.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "pageSize"));
                getTotalPageNum.ReturnType = new CodeTypeReference(typeof(int));
                //方法体
                getTotalPageNum.Statements.Add(new CodeSnippetStatement(threeTabs + $"return (context.{dbSet}.Count() + pageSize - 1) / pageSize;"));
                service.Members.Add(getTotalPageNum);
                #endregion

                //写到文件
                CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
                CodeGeneratorOptions options = new CodeGeneratorOptions();
                options.BracingStyle = "C";
                options.BlankLinesBetweenMembers = true;
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }
                using (var sw = new StreamWriter(outputPath + type.Name + "PageService.cs", false))
                {
                    provider.GenerateCodeFromCompileUnit(unit, sw, options);
                }
            }
        }
    }
}
