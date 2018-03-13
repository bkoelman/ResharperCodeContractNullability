# CNUL: Resharper nullability annotation(s) can be converted to C# syntax

## Cause
A member or parameter contains Resharper nullability annotations, while the project is configured to enable C# nullable reference types.

## Rule description
Nullable reference types are a new language feature in C# v8. It supersedes the nullability annotations that were introduced by Resharper. This rule offers to convert existing code that uses the Resharper attribute to the new C# syntax.

## How to fix violations
Run the associated code fixer by selecting "Convert to C# syntax". It is recommended to run on entire document, project or solution (if memory allows).
Remember to uninstall the ResharperCodeContractNullability package after all your code has been converted.

## When to suppress warnings
There is no technical reason to suppress this warning.

## Example of a violation

### Description
The type `C` defines members and parameters that are decorated with Resharper annotations.

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
        [CanBeNull]
        [ItemNotNull]
        private readonly List<string> _f = new List<string>();

        [CanBeNull]
        [ItemNotNull]
        public ICollection<string> P => _f?.AsReadOnly();

        [NotNull]
        [ItemNotNull]
        public Lazy<string> M([NotNull] [ItemNotNull] Task<string> p)
        {
            _f?.Add(p.Result);
            return new Lazy<string>(() => p.Result);
        }
    }
}
```

## Example of how to fix

### Description
All annotated members and parameters of the type `C` are now converted.

### Code

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace N
{
    public class C
    {
        private readonly List<string>? _f = new List<string>();

        public ICollection<string>? P => _f?.AsReadOnly();

        public Lazy<string> M(Task<string> p)
        {
            _f?.Add(p.Result);
            return new Lazy<string>(() => p.Result);
        }
    }
}
```

## Related rules

RNUL: [Member is missing nullability annotation](RNUL_MemberIsMissingNullabilityAnnotation.md)

RINUL: [Member is missing item nullability annotation](RINUL_MemberIsMissingItemNullabilityAnnotation.md)
