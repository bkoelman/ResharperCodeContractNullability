@echo off
rd /s /q coverage 2>nul
md coverage
"%USERPROFILE%\.nuget\packages\OpenCover\4.6.519\tools\OpenCover.Console.exe" -register:user -target:"%USERPROFILE%\.nuget\packages\xunit.runner.console\2.2.0\tools\xunit.console.x86.exe" -targetargs:".\CodeContractNullability\CodeContractNullability.Test\bin\Debug\CodeContractNullability.Test.dll -noshadow" -filter:"+[CodeContractNullability]*  -[CodeContractNullability]CodeContractNullability.Properties.*" -excludebyattribute:*.ExcludeFromCodeCoverage* -hideskipped:All -output:.\coverage\CodeContractNullabilityCoverage.xml
"%USERPROFILE%\.nuget\packages\ReportGenerator\2.5.10\tools\ReportGenerator.exe" -targetdir:.\coverage -reports:.\coverage\CodeContractNullabilityCoverage.xml
