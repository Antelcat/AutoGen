# Antelcat.AutoGen

Auto generate anything you want
> ~~Unless we aren't able to~~

## Supports

### `Antelcat.AutoGen.ComponentModel` :  

+ `[GenerateStringTo(string, Accessibility)]`

    Auto generate 'string To' extensions

    which is valid on `assembly` and `static partial class`

    ![GenerateStringTo](./docs/GenerateStringTo.png)

+ `Entity`

  + `[GenerateMapTo(Type, Accessibility)]`

    Auto generate mapping function to target type

    which is valid only on `partial class`

    ![GenerateStringTo](./docs/GenerateMapTo.png)

  + `[MapToName(string, Type)]`

    Specified property name when mapping to target type

    which is valid only on `property`

  + `[MapIgnore(params Type[])]`

    Ignored when generate mapping fun, if given type, only be ignored when mapping to these types

    which is valid only on `property`
