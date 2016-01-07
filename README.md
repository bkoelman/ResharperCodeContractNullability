# Resharper Code Contract Nullability

[![Build status](https://ci.appveyor.com/api/projects/status/bw9h05bekojslnmr?svg=true)](https://ci.appveyor.com/project/bkoelman/resharpercodecontractnullability) [![resharpercodecontractnullability MyGet Build Status](https://www.myget.org/BuildSource/Badge/resharpercodecontractnullability?identifier=17a3b649-fcf5-4dfb-825b-c89644271895)](https://www.myget.org/)

This Visual Studio analyzer supports you in consequently annotating your codebase with Resharpers nullability attributes. Doing so improves the [nullability analysis engine in Resharper](https://www.jetbrains.com/resharper/help/Code_Analysis__Code_Annotations.html), so `NullReferenceException`s at runtime will become something from the past.

## Get started

* You need [Visual Studio](https://www.visualstudio.com/) 2015 and [Resharper](https://www.jetbrains.com/resharper/) v9 (or higher) to use this analyzer. See [here](https://github.com/bkoelman/ResharperCodeContractNullabilityFxCop/) if you use Visual Studio 2013 or lower.

* From the NuGet package manager console:

  `Install-Package ResharperCodeContractNullability`

  `Install-Package JetBrains.Annotations`

* Rebuild your solution

Alternatively, you can install as a Visual Studio Extension from the [Visual Studio Gallery](https://visualstudiogallery.msdn.microsoft.com/97bdc5f4-f209-4441-a313-2c6e92631eaf).

Instead of adding the JetBrains package, you can [put the annotation definitions directly in your source code](https://www.jetbrains.com/resharper/help/Code_Analysis__Annotations_in_Source_Code.html). In that case, it's recommended to set the `conditional` option checked.

To make analysis work over multiple projects in your solution, define the `JETBRAINS_ANNOTATIONS` conditional compilation symbol in your project properties.

![Analyzer in action](https://github.com/bkoelman/ResharperCodeContractNullability/blob/gh-pages/images/analyzer-in-action.png)

## Trying out the latest build

After each commit, a new prerelease NuGet package is automatically published to MyGet. To try it out, follow the next steps:

* In Visual Studio: Tools, Options, NuGet Package Manager, Package Sources
    * Click **+**
    * Name: **MyGet**, Source: **http://www.myget.org/F/resharpercodecontractnullability**
    * Click **Update**, **Ok**
* In Visual Studio, select **MyGet** as package source in the GUI or Package Manager Console.
