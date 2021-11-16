# EFPlusDemo
这是一个简单的自动生成业务层代码的程序。
## 使用方法
在项目根目录下新建一个名为"genConfig.json"的文件：
```
{
  "namespace": "EFPlusDemo.Model"
}

```
namespace属性对应的是EF框架生成的实体类所在的命名空间（不要在该命名空间下放其他无关的类）。  
右键点击genConfig.json，选择“属性”，然后在“复制到输出目录”一栏点击选择“始终复制”或“如果较新则复制”。  
最后调用BLLGenerator类的GenerateCode方法，就会在运行目录下的BLL目录里创建对应的业务层代码。
