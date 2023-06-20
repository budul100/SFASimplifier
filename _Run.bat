@echo off

set /p "INPUTFILE=Enter input path: "
for %%f in ("%INPUTFILE%") do set OUTPUTFILE=%%~dpnf_.geojson

echo.
echo Simplify %INPUTFILE% into %OUTPUTFILE%
echo.

.\SFASimplifierCLI\bin\Debug\net6.0\SFASimplifierCLI.exe -i "%INPUTFILE%" -o "%OUTPUTFILE%"