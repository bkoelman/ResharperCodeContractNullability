# Resharper Code Contract Nullability

This Visual Studio analyzer supports you in consequently annotating your codebase with Resharpers nullability attributes. Doing so improves the [nullability analysis engine in Resharper](https://www.jetbrains.com/resharper/help/Code_Analysis__Code_Annotations.html), so `NullReferenceException`s at runtime will become something from the past.

## Get started

* You need [Visual Studio 2015](https://www.visualstudio.com/) and [Resharper 9](https://www.jetbrains.com/resharper/) to use this analyzer.

* From the NuGet package manager console:

  `Install-Package ResharperCodeContractNullability`

  `Install-Package JetBrains.Annotations`

* Rebuild your solution

Instead of adding the JetBrains package, you can [put the annotation definitions directly in your source code](https://www.jetbrains.com/resharper/help/Code_Analysis__Annotations_in_Source_Code.html). In that case, it's recommended to set the `conditional` option checked.

![Analyzer in action](https://github.com/bkoelman/ResharperCodeContractNullability/blob/gh-pages/images/analyzer-in-action.png)
