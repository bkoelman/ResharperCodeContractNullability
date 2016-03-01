# RINUL: Member is missing item nullability annotation

## Cause
The item of a container member or parameter technically can contain `null`, but it is not indicated whether callers should account for a `null` value.

## Rule description
Resharper performs nullability analysis and issues warnings, such as missing or redundant null checks. 
Annotating container members and parameters, such as collections, tasks and lazy types, with nullability attributes improves the results of that analysis engine.

## How to fix violations
Annotate the target member or parameter with `JetBrains.Annotations.ItemCanBeNullAttribute` or `JetBrains.Annotations.ItemNotNullAttribute`.

## When to suppress warnings
There is no technical reason to suppress this warning.

## Example of a violation

### Description
The type `C` defines container members and parameters that can contain `null`, which are not annotated.

### Code
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace N
{
    public class C
    {
        private readonly List<string> _f = new List<string>();

        public ICollection<string> P => _f.AsReadOnly();

        public Lazy<string> M(Task<string> p)
        {
            _f.Add(p.Result);
            return new Lazy<string>(() => p.Result);
        }
    }
}
```

## Example of how to fix

### Description
All container members of the type `C` that can contain `null` are now annotated.

### Code

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace N
{
    public class C
    {
        [ItemCanBeNull] private readonly List<string> _f = new List<string>();

        [ItemCanBeNull] public ICollection<string> P => _f.AsReadOnly();

        [ItemNotNull] public Lazy<string> M([ItemNotNull] Task<string> p)
        {
            _f.Add(p.Result);
            return new Lazy<string>(() => p.Result);
        }
    }
}
```

## Related rules

RNUL: [Member is missing nullability annotation](RNUL_MemberIsMissingNullabilityAnnotation.md)

XNUL: [Suggest to disable reporting on nullable value types](XNUL_SuggestToDisableReportingOnNullableValueTypes.md)
