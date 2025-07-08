@echo off
echo ========================================
echo SmartBox-Next Cleanup Script
echo ========================================
echo.

echo Renaming directories...
echo.

echo 1. Renaming smartbox-wpf-clean to smartbox-wpf...
if exist smartbox-wpf-clean (
    if exist smartbox-wpf (
        echo    ERROR: smartbox-wpf already exists!
    ) else (
        ren smartbox-wpf-clean smartbox-wpf
        if errorlevel 1 (
            echo    FAILED - Files may be locked
        ) else (
            echo    SUCCESS
        )
    )
) else (
    echo    SKIPPED - smartbox-wpf-clean not found
)

echo.
echo 2. Renaming smartbox-winui3 to smartbox-winui3-reference...
if exist smartbox-winui3 (
    if exist smartbox-winui3-reference (
        echo    ERROR: smartbox-winui3-reference already exists!
    ) else (
        ren smartbox-winui3 smartbox-winui3-reference
        if errorlevel 1 (
            echo    FAILED - Files may be locked
        ) else (
            echo    SUCCESS
        )
    )
) else (
    echo    SKIPPED - smartbox-winui3 not found
)

echo.
echo 3. Removing smartbox-wpf-new...
if exist smartbox-wpf-new (
    rmdir /s /q smartbox-wpf-new
    if errorlevel 1 (
        echo    FAILED - Files may be locked
    ) else (
        echo    SUCCESS
    )
) else (
    echo    SKIPPED - smartbox-wpf-new not found
)

echo.
echo ========================================
echo Cleanup complete!
echo.
echo Final structure should be:
echo   - smartbox-wpf (main working directory)
echo   - smartbox-winui3-reference (for reference)
echo   - archive/ (old versions)
echo ========================================
echo.
pause