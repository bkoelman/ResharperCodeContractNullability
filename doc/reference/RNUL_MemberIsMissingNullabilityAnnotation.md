# RNUL: Member is missing nullability annotation

## Cause
A member or parameter technically can contain `null`, but it is not indicated whether callers should account for a `null` value.

## Rule description
Resharper performs nullability analysis and issues warnings, such as missing or redundant null checks. 
Annotating members and parameters with nullability attributes improves the results of that analysis engine.

## How to fix violations
Annotate the target member or parameter with `JetBrains.Annotations.CanBeNullAttribute` or `JetBrains.Annotations.NotNullAttribute`.

## When to suppress warnings
There is no technical reason to suppress this warning.

## Example of a violation

### Description
The type `C` defines members and parameters that can contain `null`, which are not annotated.

### Code
```csharp
namespace N
{
    public class C
    {
        private string _f;

        public string P => _f;

        public string M(string p)
        {
            _f = p;
            return p;
        }
    }
}
```

## Example of how to fix

### Description
All members of the type `C` that can contain `null` are now annotated.

### Code

```csharp
using JetBrains.Annotations;

namespace N
{
    public class C
    {
        [CanBeNull] private string _f;

        [CanBeNull] public string P => _f;

        [NotNull] public string M([NotNull] string p)
        {
            _f = p;
            return p;
        }
    }
}
```

## Related rules

RINUL: [Member is missing item nullability annotation](RINUL_MemberIsMissingItemNullabilityAnnotation.md)

XNUL: [Suggest to disable reporting on nullable value types](XNUL_SuggestToDisableReportingOnNullableValueTypes.md)
