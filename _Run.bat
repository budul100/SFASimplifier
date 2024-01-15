@echo off

SET SFASimplifierPath=.\_Publish\SFASimplifier.exe

echo.
set /p "INPUTFILE=Enter path of geojson file: "

for %%f in ("%INPUTFILE%") do set FEATURESFILE=%%~dpnf_.geojson

echo.
set /p "BBOX=Set bounding box coordinates (or leave empty): "

echo.
echo Simplify %INPUTFILE%
echo.

IF "%BBOX%"=="" ( 
	%SFASimplifierPath% -i "%INPUTFILE%" -o "%FEATURESFILE%" --pointattrfilter public_transport,stop_position,public_transport,station,railway,halt,railway,station,railway,stop,subway,halt,subway,station,subway,stop
) ELSE (
	%SFASimplifierPath% -i "%INPUTFILE%" -o "%FEATURESFILE%" --bboxfilter "%BBOX%" --pointattrfilter public_transport,stop_position,public_transport,station,railway,halt,railway,station,railway,stop,subway,halt,subway,station,subway,stop
)

echo.
pause