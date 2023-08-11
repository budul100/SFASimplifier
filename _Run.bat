@echo off

echo.
set /p "INPUTFILE=Enter input path: "

for %%f in ("%INPUTFILE%") do set FEATURESFILE=%%~dpnf_.geojson

echo.
echo Simplify %INPUTFILE%
echo.

.\SFASimplifierCLI\bin\Debug\net6.0\SFASimplifierCLI.exe -i "%INPUTFILE%" -o "%FEATURESFILE%"