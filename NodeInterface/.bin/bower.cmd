@echo off
SET PATH=%~dp0;%PATH%
"%~dp0node" "%~dp0..\..\packages\Bower.1.3.11\node_modules\bower\bin\bower" %*
