@echo off
@setlocal
pushd
set ERROR_CODE=0
dotnet build src\Grover.CLI\Grover.CLI.csproj %*

:end
popd
exit /B %ERROR_CODE%