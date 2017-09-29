@echo off
rd /s /q coverage 2>nul
md coverage

set configuration=Debug
set opencover="%USERPROFILE%\.nuget\packages\OpenCover\4.6.519\tools\OpenCover.Console.exe"
set reportgenerator="%USERPROFILE%\.nuget\packages\ReportGenerator\3.0.0\tools\ReportGenerator.exe"
set testrunner="%USERPROFILE%\.nuget\packages\xunit.runner.console\2.2.0\tools\xunit.console.x86.exe"
set target=".\src\CodeContractNullability\CodeContractNullability.Test\bin\%configuration%\net452\CodeContractNullability.Test.dll -noshadow"
set filter="+[CodeContractNullability*]*  -[CodeContractNullability.Test*]*"
set coveragefile=".\coverage\CodeCoverage.xml"

%opencover% -register:user -target:%testrunner% -targetargs:%target% -filter:%filter% -hideskipped:All -output:%coveragefile%
%reportgenerator% -targetdir:.\coverage -reports:%coveragefile%

