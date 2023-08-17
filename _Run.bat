@echo off

echo.
set /p "INPUTFILE=Enter path of geojson file: "

for %%f in ("%INPUTFILE%") do set FEATURESFILE=%%~dpnf_.geojson

echo.
echo Simplify %INPUTFILE%
echo.

.\_Publish\SFASimplifier.exe -i "%INPUTFILE%" -o "%FEATURESFILE%"

echo.
pause