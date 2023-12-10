# Antelcat.AutoGen

自动生成任何你想要的东西
> ~~除非做不到~~

## 已支持

### `Antelcat.AutoGen.ComponentModel` :  

+ #### `[GenerateStringTo(string, Accessibility)]` :  

    自动生成 string To 的扩展

    仅能在 `assembly` 和 `static partial class` 上使用

    ![GenerateStringTo](./docs/GenerateStringTo.png)

+ #### `Entity` :  

  + #### `[GenerateMapTo(Type, Accessibility)]` :  

    自动生成与其他类型的映射代码

    只能在 `partial class` 上使用

    ![GenerateStringTo](./docs/GenerateMapTo.png)

    > 你可以使用它生成 `浅拷贝`

  + #### `[MapToName(string, Type)]` :  

    指定在目标对象上映射的属性名

  + #### `[MapIgnore(params Type[])]` :  

    在生成映射代码时忽略，如果指定类型，则仅在生成面向目标类型时忽略
