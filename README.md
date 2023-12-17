# Antelcat.AutoGen

自动生成任何你想要的东西
> ~~除非做不到~~

## 已支持

### `Antelcat.AutoGen.ComponentModel` :  

+ #### `[GenerateStringTo(string, Accessibility)]` :  

    自动生成 string To 的扩展

    仅能在 `assembly` 和 `static partial class` 上使用

    ![GenerateStringTo](./docs/GenerateStringTo.png)

+ #### `Mapping` :  

  + #### `[GenerateMap(Accessibility)]` :  

    自动生成与其他类型的映射代码

    只能在 `partial method` 上使用

    ![GenerateMapTo](./docs/GenerateMap.png)

    > 你可以使用它生成 `浅拷贝`

  + #### `[MapBetween(string, string)]` :  

    指定在两者类型上的属性名称映射

  + #### `[MapIgnore]` :  

    在生成映射代码时忽略

  + #### `[MapInclude(string, Type)]` :  

    显式添加参与映射的被 `[MapIgnore]` 的属性

  + #### `[MapExclude(string, Type)]` :  

    将属性在映射中移除

  + #### `[MapConstructor(params string[])]` :  

    指定构造目标函数所提供的属性，如果为空则尝试自动匹配
