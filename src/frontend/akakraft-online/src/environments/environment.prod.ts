// Produktions-Konfiguration
// apiUrl: '/api' → nginx proxied /api/ intern zum Backend-Service (kein hardcoded Hostname nötig)
export const environment = {
  production: true,
  apiUrl: '/api',
  firebase: {
    apiKey: 'AIzaSyC6h5Vb0L1GNByiR2DmG-PGP3bKswSz1ok',
    authDomain: 'akakraft-online-ffbec.firebaseapp.com',
    projectId: 'akakraft-online-ffbec',
    storageBucket: 'akakraft-online-ffbec.firebasestorage.app',
    messagingSenderId: '571523249639',
    appId: '1:571523249639:web:8238bf7d0ebc9855343942',
  },
  vapidKey: 'BAlHJCIzTHjJdEr7S9RNWBsL_CA5b33DnV4xLEgHtdqBu-8LkWMm04UYkFHKxfxHcLFUTSehiuCa_KdQUzSvwjg',
  swPath: '/app/firebase-messaging-sw.js',
};
