# Rabbit.WebApiFramework

基于asp.net core构件的插件式webapi快速开发框架

##### 项目说明

- 使用asp.net core 2.2 开发，利用ApplicationPart构件插件式的WebAppFramework/WebApiFramework

##### 技术方案

- asp.net core 2.2

##### 开发环境

- Visual Studio 2017 15.9

##### 解决方案组织结构

```
+---Plugins  // 插件工程
|   \---SamplePlugin
|       +---Controllers      // 插件定义的Controller
|       \---Views           // 插件定义的View
|           \---Page
|
+---Rabbit.WebApiFramework // 服务主工程
|   +---Controllers
|   +---Models
|   +---Plugins            // 插件发布存放位置
|   |   +---SamplePlugin   
|   |   |   \---netcoreapp2.2
|   +---Views
|   |   +---Home
|   |   \---Shared
|   \---wwwroot
|       +---css
|       +---js
|       \---lib
|
\---Rabbit.WebApiFramework.Core   // 通用部分
    +---Interface
```

##### 关键点

```c#
var assemblyLst = LoadPlugins();

 // 添加View的提供方式
 services.Configure<RazorViewEngineOptions>(options =>
            {
                foreach (var ass in assemblyLst)
                    options.FileProviders.Add(new EmbeddedFileProvider(ass));
            });
// 添加Controller
services.AddMvc().ConfigureApplicationPartManager(manager =>
            {
                foreach (var ass in assemblyLst)
                    manager.ApplicationParts.Add(new AssemblyPart(ass));
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
```

##### 如何添加插件

1. 新建**dotnetcore 2.2**的工程放置在**Plugins**（推荐目录）目录
2. 修改插件生成的输出路径到**Rabbit.WebApiFramework\Plugins**，**注意**：需要设置成相对路径
3. 创建**Controller**和**View**文件夹防止**控制器**和**视图**
4. **新建视图**需要修改**cshtml**文件的生成操作为**嵌入资源（Embed Resource）**,否则无法查询到插件的视图

