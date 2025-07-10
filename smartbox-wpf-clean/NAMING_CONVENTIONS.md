# SmartBoxNext Naming Conventions

## Overview
This document establishes the naming conventions used throughout the SmartBoxNext codebase to ensure consistency and prevent data binding issues.

## Established Conventions

### 1. C# Code (Backend)
- **Properties**: PascalCase (e.g., `ServerHost`, `CalledAeTitle`)
- **Methods**: PascalCase (e.g., `HandleTestPacsConnection`)
- **Private fields**: _camelCase (e.g., `_logger`, `_config`)
- **Local variables**: camelCase (e.g., `serverHost`, `success`)

### 2. JSON Communication (C# ↔ JavaScript)
- **All properties**: PascalCase to match C# models
- **Examples**:
  ```json
  {
    "ServerHost": "localhost",
    "ServerPort": 104,
    "CalledAeTitle": "ORTHANC",
    "CallingAeTitle": "SMARTBOX"
  }
  ```

### 3. Configuration Files (config.json)
- **All properties**: PascalCase
- **Nested objects**: PascalCase
- **Example**:
  ```json
  {
    "Storage": {
      "PhotosPath": "./Data/Photos",
      "MaxStorageDays": 30
    },
    "Pacs": {
      "ServerHost": "localhost",
      "ServerPort": 104
    }
  }
  ```

### 4. JavaScript (Frontend)
- **Variables/functions**: camelCase (e.g., `testPacsConnection`, `pacsConfig`)
- **Classes**: PascalCase (e.g., `SettingsManager`)
- **Data sent to C#**: PascalCase properties
- **Example**:
  ```javascript
  const pacsConfig = {
    ServerHost: "localhost",      // PascalCase for C# compatibility
    ServerPort: 104,
    CalledAeTitle: "ORTHANC"
  };
  ```

### 5. HTML
- **Element IDs**: kebab-case (e.g., `pacs-server-host`, `test-mwl`)
- **CSS classes**: kebab-case (e.g., `test-button`, `setting-group`)
- **Data attributes**: kebab-case (e.g., `data-section`)

## Special Cases

### 1. Abbreviations
- **AET** (Application Entity Title): Always written as `AET` not `AeTitle`
  - C#: `MwlServerAET`, `CalledAeTitle` (inconsistent - needs future fix)
  - JSON: Follow C# property name exactly

### 2. Compound Words
- **Worklist**: Single word (e.g., `EnableWorklist`, not `EnableWorkList`)
- **WebView**: PascalCase within (e.g., `WebView2`, not `Webview2`)

### 3. Message Actions
- **WebView2 message actions**: lowercase (e.g., `testpacsconnection`, `savesettings`)
- **Response actions**: camelCase (e.g., `testConnectionResult`)

## Conversion Functions

### HTML ID to Property Name
The `htmlIdToPropertyPath` function in settings.js converts:
- `pacs-server-host` → `Pacs.ServerHost`
- `mwlsettings-enable-worklist` → `MwlSettings.EnableWorklist`

## Common Pitfalls to Avoid

1. **Don't mix camelCase and PascalCase** in JSON communication
2. **Don't use snake_case** anywhere in the codebase
3. **Don't abbreviate inconsistently** (use full words or consistent abbreviations)
4. **Always match the C# property name exactly** when sending data from JavaScript

## Validation Checklist

Before committing code, verify:
- [ ] JavaScript sends PascalCase properties to C#
- [ ] C# expects PascalCase properties from JavaScript
- [ ] HTML IDs use kebab-case
- [ ] config.json uses PascalCase
- [ ] No case mismatches in data binding

## Future Improvements

1. Consider using JSON serializer attributes to handle automatic case conversion
2. Create TypeScript definitions from C# models
3. Add unit tests for data serialization/deserialization