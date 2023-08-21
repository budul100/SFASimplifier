@echo off

SET OutputDir1=%USERPROFILE%\OneDrive - IVU Traffic Technologies AG\Geodata\SFASimplifier
SET OutputDir2=\\ivu-ag.com\storage\tausch-bln\mgr\Geodata\SFASimplifier

SET DeploymentPath=.\_Publish

echo.
echo ### Clean up ###
echo.

pushd .\Additionals\Scripts

call Clean.bat

popd

echo.
echo ### Build application ###
echo.

dotnet publish CLI -p:DebugType=None -p:DebugSymbols=false -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -p:PublishSingleFile=true --self-contained --configuration Release --runtime win-x64 --output _Publish

rem dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true

echo.
echo ### Deploy application ###
echo.

copy /y "%DeploymentPath%\*.*" "%OutputDir1%\"
copy /y "%DeploymentPath%\*.*" "%OutputDir2%\"