@echo off
@setlocal
pushd
set ERROR_CODE=0
src\Grover.CLI\bin\Debug\net6.0\Grover.CLI.exe %*

:end
popd
exit /B %ERROR_CODE%