@echo off
rd /s /q coverage 2>nul
md coverage
.\packages\OpenCover.4.6.519\tools\OpenCover.Console.exe -register:user -target:".\packages\xunit.runner.console.2.1.0\tools\xunit.console.x86.exe" -targetargs:".\CodeContractNullability\CodeContractNullability.Test\bin\Debug\CodeContractNullability.Test.dll -noshadow" -filter:"+[CodeContractNullability]*  -[CodeContractNullability]CodeContractNullability.Properties.*" -excludebyattribute:*.ExcludeFromCodeCoverage* -hideskipped:All -output:.\coverage\CodeContractNullabilityCoverage.xml
.\packages\ReportGenerator.2.5.2\tools\ReportGenerator.exe -targetdir:.\coverage -reports:.\coverage\CodeContractNullabilityCoverage.xml
