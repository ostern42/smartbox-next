# Git Workflow für SmartBox-Next

## WICHTIG: Git First!
**IMMER** als erstes `git log` und CHANGELOG.md checken bevor irgendwas anderes!

## Standard Workflow

### 1. Session Start
```bash
git status
git log --oneline -10
cat CHANGELOG.md
```

### 2. Während der Arbeit
- Kleine, atomare Commits
- Aussagekräftige Commit Messages
- Nach jedem Feature committen

### 3. Commit Format
```
<type>: <description>

<body>

🤖 Generated with [opencode](https://opencode.ai)

Co-Authored-By: opencode <noreply@opencode.ai>
```

Types:
- feat: Neues Feature
- fix: Bugfix
- docs: Dokumentation
- refactor: Code-Verbesserung
- test: Tests
- chore: Maintenance

### 4. CHANGELOG Update
Bei jedem signifikanten Change:
1. CHANGELOG.md updaten
2. Version nach Semantic Versioning
3. Datum nicht vergessen

### 5. Session Ende
```bash
git status
git add -A
git commit -m "chore: Update session documentation"
```

## Automatisierung
TODO: Pre-commit hooks für:
- CHANGELOG.md check
- .wisdom/ updates
- Automatic timestamps

## Warum das wichtig ist
- Past-Me dokumentiert für Future-Me
- Oliver kann Progress verfolgen
- Keine "war das schon fertig?" Momente mehr
- Git log > meine Demenz