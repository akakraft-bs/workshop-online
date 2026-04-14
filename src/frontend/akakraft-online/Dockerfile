# =============================================================================
# Build-Stage: Node + Angular CLI
# =============================================================================
FROM node:22-alpine AS build
WORKDIR /app

# package.json zuerst kopieren – npm ci wird gecacht solange keine Abhängigkeiten
# sich ändern.
COPY package*.json ./
RUN npm ci --ignore-scripts

# Quellcode kopieren und Production-Build ausführen
# (nutzt environment.prod.ts via fileReplacements in angular.json)
COPY . .
RUN npm run build -- --configuration production

# =============================================================================
# Runtime-Stage: nginx (schlank, nur statische Dateien + Proxy)
# =============================================================================
FROM nginx:alpine AS runtime

# Angular-Build-Output in nginx-Webroot kopieren
COPY --from=build /app/dist/akakraft-online/browser /usr/share/nginx/html

# nginx-Template: ${BACKEND_URL} wird beim Start per envsubst ersetzt.
# Das offizielle nginx-Image verarbeitet /etc/nginx/templates/ automatisch.
COPY nginx.default.conf.template /etc/nginx/templates/default.conf.template

EXPOSE 80

# Standardwert für lokale Docker-Compose-Nutzung.
# In K8s via Deployment-Env überschreiben:
#   BACKEND_URL=http://akakraft-backend:8080
ENV BACKEND_URL=http://backend:8080
ENV MINIO_URL=http://minio:9000
