importScripts('https://www.gstatic.com/firebasejs/10.12.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.12.0/firebase-messaging-compat.js');

firebase.initializeApp({
  apiKey: 'AIzaSyC6h5Vb0L1GNByiR2DmG-PGP3bKswSz1ok',
  authDomain: 'akakraft-online-ffbec.firebaseapp.com',
  projectId: 'akakraft-online-ffbec',
  storageBucket: 'akakraft-online-ffbec.firebasestorage.app',
  messagingSenderId: '571523249639',
  appId: '1:571523249639:web:8238bf7d0ebc9855343942',
});

const messaging = firebase.messaging();

// Hintergrund-Nachrichten (App ist nicht im Vordergrund).
// Die Nachricht ist data-only (kein notification-Objekt), daher zeigt der Browser
// sie nicht automatisch – wir zeigen sie hier genau einmal.
messaging.onBackgroundMessage(payload => {
  const title = payload.data?.title ?? 'AkaKraft';
  const body  = payload.data?.body  ?? '';

  self.registration.showNotification(title, {
    body,
    icon:  '/app/android-chrome-192x192.png',
    badge: '/app/favicon-32x32.png',
    data:  { url: payload.data?.url },
  });
});

// Klick auf Benachrichtigung → App öffnen / fokussieren
self.addEventListener('notificationclick', event => {
  event.notification.close();

  const url = event.notification.data?.url ?? '/app/';

  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(clientList => {
      for (const client of clientList) {
        if (client.url.includes('/app/') && 'focus' in client) {
          client.navigate(url);
          return client.focus();
        }
      }
      return clients.openWindow(url);
    })
  );
});
