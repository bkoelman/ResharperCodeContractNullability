# XNUL: Suggest to disable reporting on nullable value types

## Cause
[RNUL](RNUL_MemberIsMissingNullabilityAnnotation.md) or [RINUL](RINUL_MemberIsMissingItemNullabilityAnnotation.md) is reported on a member.
Some users prefer to not get nullability warnings reported on nullable value types. This rule facilitates a shortcut to suppress such warnings.

## Rule description
This pseudo-rule exists to provide a context-menu action that updates project configuration to disable reporting on nullable value types.

## How to fix violations
N/A.

## When to suppress warnings
Suppress this rule to hide the context-menu action that disables reporting on nullable value types.

## Example of a violation

### Description
The type `C` defines a field of type ``System.Nullable`1``, which is not annotated.

### Code
```csharp
namespace N
{
    public class C
    {
        private int? _f;
    }
}
```

## Example of how to fix

### Description
N/A.

## Related rules

RNUL: [Member is missing nullability annotation](RNUL_MemberIsMissingNullabilityAnnotation.md)

RINUL: [Member is missing item nullability annotation](RINUL_MemberIsMissingItemNullabilityAnnotation.md)
