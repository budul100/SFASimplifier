@echo off

echo.
set /p "INPUTFILE=Enter input path: "

for %%f in ("%INPUTFILE%") do set FEATURESFILE=%%~dpnf_.geojson

echo.
echo Simplify %INPUTFILE%
echo.

.\_Publish\SFASimplifier.exe -i "%INPUTFILE%" -o "%FEATURESFILE%"

echo.
pause