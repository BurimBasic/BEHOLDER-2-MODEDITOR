@echo off
cd /d "%~dp0"
AssetIndex.exe "%~dp0prototype\Beholder2_Data" "%~dp0prototype\asset-catalog.tsv"
pause
