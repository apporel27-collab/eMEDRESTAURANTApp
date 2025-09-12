@echo off
REM This script copies the Auth_Setup_Modified.sql to the output directory

echo Copying Auth_Setup_Modified.sql to output directory...

REM Get the current directory
SET "sourceDir=%~dp0"
SET "source=%sourceDir%SQL\Auth_Setup_Modified.sql"
SET "target=%sourceDir%bin\Debug\net6.0\SQL\"

REM Create the target directory if it doesn't exist
IF NOT EXIST "%target%" mkdir "%target%"

REM Copy the file
copy "%source%" "%target%"

echo SQL script copy complete!
