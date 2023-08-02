@echo off

set /p "INPUTFILE=Enter input path: "

for %%f in ("%INPUTFILE%") do set FEATURESFILE=%%~dpnf_Merged.geojson
for %%f in ("%INPUTFILE%") do set ROUTINGFILE=%%~dpnf_Lines.txt

echo.
echo Simplify %INPUTFILE%
echo.

.\SFASimplifierCLI\bin\Debug\net6.0\SFASimplifierCLI.exe -i "%INPUTFILE%" -f "%FEATURESFILE%" -r "%ROUTINGFILE%"