# Resharper Code Contract Nullability

[![Build status](https://ci.appveyor.com/api/projects/status/bw9h05bekojslnmr?svg=true)](https://ci.appveyor.com/project/bkoelman/resharpercodecontractnullability/branch/master)
[![codecov](https://codecov.io/gh/bkoelman/ResharperCodeContractNullability/branch/master/graph/badge.svg)](https://codecov.io/gh/bkoelman/ResharperCodeContractNullability)

This Visual Studio analyzer supports you in consequently annotating your codebase with Resharpers nullability attributes. Doing so improves the [nullability analysis engine in Resharper](https://www.jetbrains.com/resharper/help/Code_Analysis__Code_Annotations.html), so `NullReferenceException`s at runtime will become something from the past.

## Get started

* You need [Visual Studio](https://www.visualstudio.com/) 2015/2017/2019 and [Resharper](https://www.jetbrains.com/resharper/) v9 (or higher) to use this analyzer. See [here](https://github.com/bkoelman/ResharperCodeContractNullabilityFxCop/) if you use Visual Studio 2013 or lower.

* From the NuGet package manager console:

  `Install-Package ResharperCodeContractNullability`

  `Install-Package JetBrains.Annotations`

* Rebuild your solution

Alternatively, you can install as a Visual Studio Extension from the [Visual Studio Gallery](https://visualstudiogallery.msdn.microsoft.com/97bdc5f4-f209-4441-a313-2c6e92631eaf).

Instead of adding the JetBrains package, you can [put the annotation definitions directly in your source code](https://www.jetbrains.com/resharper/help/Code_Analysis__Annotations_in_Source_Code.html). In that case, it's [recommended](http://blog.jetbrains.com/dotnet/2015/08/12/how-to-use-jetbrains-annotations-to-improve-resharper-inspections/) to set the `conditional` and/or `internal` options checked.

To make analysis work over multiple projects in your solution, define the `JETBRAINS_ANNOTATIONS` conditional compilation symbol in your project properties.

![Analyzer in action](https://github.com/bkoelman/ResharperCodeContractNullability/blob/gh-pages/images/analyzer-in-action.png)

## Trying out the latest build

After each commit, a new prerelease NuGet package is automatically published to AppVeyor at https://ci.appveyor.com/project/bkoelman/resharpercodecontractnullability/branch/master/artifacts. To try it out, follow the next steps:

* In Visual Studio: **Tools**, **NuGet Package Manager**, **Package Manager Settings**, **Package Sources**
    * Click **+**
    * Name: **AppVeyor ResharperCodeContractNullability**, Source: **https://ci.appveyor.com/nuget/resharpercodecontractnullability**
    * Click **Update**, **Ok**
* Open the NuGet package manager console (**Tools**, **NuGet Package Manager**, **Package Manager Console**)
    * Select **AppVeyor ResharperCodeContractNullability** as package source
    * Run command: `Install-Package ResharperCodeContractNullability -pre`

## Running on your build server

This assumes your project uses ResharperCodeContractNullability via NuGet, but Resharper is not installed on your build server. To make the analyzer succeed there, simply add another NuGet reference to your project.

* From the NuGet package manager console:

  `Install-Package JetBrains.ExternalAnnotations -Version 10.2.29`
