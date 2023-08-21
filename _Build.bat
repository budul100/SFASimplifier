@echo off

SET OutputDir1=%USERPROFILE%\OneDrive - IVU Traffic Technologies AG\Geodata\SFASimplifier
SET OutputDir2=\\ivu-ag.com\storage\tausch-bln\mgr\Geodata\SFASimplifier

SET DeploymentPath=.\_Publish

echo.
echo ### Clean up ###
echo.

pushd .\Additionals\Scripts

call Clean.bat

if not %ERRORLEVEL% == 0 goto End

popd

echo.
echo ### Build application ###
echo.

dotnet publish CLI -p:DebugType=None -p:DebugSymbols=false -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -p:PublishSingleFile=true --self-contained --configuration Release --runtime win-x64 --output _Publish

rem dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true

if not %ERRORLEVEL% == 0 goto End

echo.
echo ### Deploy application ###
echo.

copy /y "%DeploymentPath%\*.*" "%OutputDir1%\"
copy /y "%DeploymentPath%\*.*" "%OutputDir2%\"

if not %ERRORLEVEL% == 0 goto End

echo.
echo.
echo COMPILING AND DEPLOYING WAS SUCCESFULL.
echo.
echo.

:End

pause