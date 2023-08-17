@ECHO off

pushd %~dp0..\..

RMDIR /S /Q ".\_Publish"

FOR /F "tokens=*" %%G IN ('DIR /B /AD /a-h /S bin') DO RMDIR /S /Q "%%G"
FOR /F "tokens=*" %%G IN ('DIR /B /AD /a-h /S obj') DO RMDIR /S /Q "%%G"

for /f "usebackq delims=" %%d in (`"dir /ad/b/s | sort /R"`) do rd "%%d"

popd