@echo off
rd /s /q coverage 2>nul
md coverage

set configuration=Debug
set opencover="%USERPROFILE%\.nuget\packages\OpenCover\4.6.519\tools\OpenCover.Console.exe"
set reportgenerator="%USERPROFILE%\.nuget\packages\ReportGenerator\4.1.4\tools\net47\ReportGenerator.exe"
set testrunner="%USERPROFILE%\.nuget\packages\xunit.runner.console\2.4.1\tools\net472\xunit.console.x86.exe"
set target=".\src\CodeContractNullability\CodeContractNullability.Test\bin\%configuration%\net472\CodeContractNullability.Test.dll -noshadow"
set filter="+[CodeContractNullability*]*  -[CodeContractNullability.Test*]*"
set coveragefile=".\coverage\CodeCoverage.xml"

%opencover% -register:user -target:%testrunner% -targetargs:%target% -filter:%filter% -hideskipped:All -output:%coveragefile%
%reportgenerator% -targetdir:.\coverage -reports:%coveragefile%

