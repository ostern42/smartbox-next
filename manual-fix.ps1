# SmartBox Next - Manual Fix Commands
# Führe diese Befehle einzeln aus

# 1. Wails Projekt erstellen (falls noch nicht geschehen)
wails init -n smartbox-next -t vue

# 2. In Projekt wechseln
cd smartbox-next

# 3. Backend-Ordner erstellen
mkdir backend\capture
mkdir backend\dicom
mkdir backend\overlay
mkdir backend\license
mkdir backend\api
mkdir backend\trigger

# 4. Go Module initialisieren
go mod init smartbox-next
go mod tidy

# 5. Go Dependencies
go get github.com/suyashkumar/dicom
go get github.com/gin-gonic/gin
go get github.com/gorilla/websocket

# 6. Frontend Dependencies
cd frontend
npm install
npm install pinia @vueuse/core
npm install -D @types/node

# 7. Zurück und testen
cd ..
wails dev