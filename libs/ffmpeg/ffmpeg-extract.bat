@echo off
setlocal EnableDelayedExpansion

REM This script automatically extracts the downloaded FFmpeg 7-zip archives and sets up the required folder structure

SET version=ffmpeg-2.8.2
SET platforms="win32" "win64"
SET disttypes="dev" "shared"


REM 7-Zip
REM echo without newline at end
ECHO|SET /p=Searching for 7-zip... 

WHERE 7z >nul 2>nul
IF %ERRORLEVEL% EQU 0 (
    ECHO found on path
    SET sevenzip=7z
) ELSE (
    IF EXIST "%PROGRAMFILES%\7-zip\7z.exe" (
        ECHO found in program files
        SET sevenzip="%PROGRAMFILES%\7-zip\7z.exe"
    ) ELSE (
        IF EXIST "%PROGRAMFILES(X86)%\7-zip\7z.exe" (
            ECHO found in x86 program files
            SET sevenzip="%PROGRAMFILES(X86)%\7-zip\7z.exe"
        ) ELSE (
            ECHO NOT FOUND
            ECHO ERROR: 7-zip not found... please install to default directory and retry, or extract files manually according to the instructions in ffmpeg-prepare.txt
            GOTO end
        )
    )
)


REM FFmpeg archive files
ECHO Looking for required archive files...
SET filenotfound=0

FOR %%p in (%platforms%) DO (
    FOR %%d in (%disttypes%) DO (
        SET file=%version%-%%~p-%%~d.7z

        REM echo without newline at end
        ECHO|SET /p=Checking !file!... 

        IF NOT EXIST !file! (
            ECHO MISSING
            SET filenotfound=1
        ) ELSE (
            ECHO ok
        )
    )
)

IF %filenotfound% EQU 1 (
    ECHO ERROR: Missing file^(s^)^^! Please download the required files into this script's directory^^!
    GOTO end
)


REM Extraction
ECHO Extracting...

FOR %%p in (%platforms%) DO (
    REM create target dir, suppress errors if dir already exists
    mkdir %%~p >nul 2>nul

    FOR %%d in (%disttypes%) DO (
        REM assemble archive name
        SET name=%version%-%%~p-%%~d

        REM extract archive without cmd output
        %sevenzip% x -y !name!.7z >nul

        REM move necessary folders from extracted archive to target folder
        REM (not every archive contains every folder)
        FOR %%d in (bin include lib) DO (
            IF EXIST !name!\%%~d (
                move !name!\%%~d %%~p\%%~d >nul
            )
        )

        REM force delete remainder of extracted folder
        rmdir /S /Q !name!
    )
)

ECHO Finished^^! FFmpeg is setup correctly^^! You can now safely delete the downloaded archive files.

:end
REM wait for key press, to display results also to users calling this script directly (not from a prompt)
pause
