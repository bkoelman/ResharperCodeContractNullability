@echo off

rmdir /s /q .vs 1>NUL 2>NUL
rmdir /s /q CodeContractNullability\CodeContractNullability\bin 1>NUL 2>NUL
rmdir /s /q CodeContractNullability\CodeContractNullability\obj 1>NUL 2>NUL
rmdir /s /q CodeContractNullability\CodeContractNullability.Test\bin 1>NUL 2>NUL
rmdir /s /q CodeContractNullability\CodeContractNullability.Test\obj 1>NUL 2>NUL
rmdir /s /q CodeContractNullability\CodeContractNullability.Vsix\bin 1>NUL 2>NUL
rmdir /s /q CodeContractNullability\CodeContractNullability.Vsix\obj 1>NUL 2>NUL
rmdir /s /q TestResults 1>NUL 2>NUL
rmdir /s /q packages 1>NUL 2>NUL

del /f /s /q *.user 1>NUL 2>NUL
