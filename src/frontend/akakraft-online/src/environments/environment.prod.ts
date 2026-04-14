// Produktions-Konfiguration
// apiUrl: '/api' → nginx proxied /api/ intern zum Backend-Service (kein hardcoded Hostname nötig)
export const environment = {
  production: true,
  apiUrl: '/api',
};
