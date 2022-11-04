// File generated by FlutterFire CLI.
// ignore_for_file: lines_longer_than_80_chars, avoid_classes_with_only_static_members
import 'package:firebase_core/firebase_core.dart' show FirebaseOptions;
import 'package:flutter/foundation.dart'
    show defaultTargetPlatform, kIsWeb, TargetPlatform;

/// Default [FirebaseOptions] for use with your Firebase apps.
///
/// Example:
/// ```dart
/// import 'firebase_options.dart';
/// // ...
/// await Firebase.initializeApp(
///   options: DefaultFirebaseOptions.currentPlatform,
/// );
/// ```
class DefaultFirebaseOptions {
  static FirebaseOptions get currentPlatform {
    if (kIsWeb) {
      return web;
    }
    switch (defaultTargetPlatform) {
      case TargetPlatform.android:
        return android;
      case TargetPlatform.iOS:
        return ios;
      case TargetPlatform.macOS:
        throw UnsupportedError(
          'DefaultFirebaseOptions have not been configured for macos - '
          'you can reconfigure this by running the FlutterFire CLI again.',
        );
      case TargetPlatform.windows:
        throw UnsupportedError(
          'DefaultFirebaseOptions have not been configured for windows - '
          'you can reconfigure this by running the FlutterFire CLI again.',
        );
      case TargetPlatform.linux:
        throw UnsupportedError(
          'DefaultFirebaseOptions have not been configured for linux - '
          'you can reconfigure this by running the FlutterFire CLI again.',
        );
      default:
        throw UnsupportedError(
          'DefaultFirebaseOptions are not supported for this platform.',
        );
    }
  }

  static const FirebaseOptions web = FirebaseOptions(
    apiKey: 'AIzaSyBjxzygRk6lcm94YX6QWMWXZ0ta0IDfce8',
    appId: '1:374682980066:web:d4aa7147ea8e1a6867d8bb',
    messagingSenderId: '374682980066',
    projectId: 'firestore-parlamento',
    authDomain: 'firestore-parlamento.firebaseapp.com',
    storageBucket: 'firestore-parlamento.appspot.com',
    measurementId: 'G-ZBL0G8DVFJ',
  );

  static const FirebaseOptions android = FirebaseOptions(
    apiKey: 'AIzaSyCE9CtEp0ixrs0mymG7kAjEjuMxwP_ulng',
    appId: '1:374682980066:android:4b9107897cbf075367d8bb',
    messagingSenderId: '374682980066',
    projectId: 'firestore-parlamento',
    storageBucket: 'firestore-parlamento.appspot.com',
  );

  static const FirebaseOptions ios = FirebaseOptions(
    apiKey: 'AIzaSyD7sNm4ShENh0mqCzcaDylNbWlSE5cHX88',
    appId: '1:374682980066:ios:663ca3634f13006467d8bb',
    messagingSenderId: '374682980066',
    projectId: 'firestore-parlamento',
    storageBucket: 'firestore-parlamento.appspot.com',
    iosClientId: '374682980066-b8b5e90rmnrkpgng5k4fufktfat7voom.apps.googleusercontent.com',
    iosBundleId: 'com.example.frontend',
  );
}
