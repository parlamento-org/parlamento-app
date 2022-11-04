import 'package:flutter/material.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:frontend/widget/get_user_example.dart';
import 'firebase_options.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  // This widget is the root of your application.
  @override
  Widget build(BuildContext context) {
    return FutureBuilder(
      // Initialize FlutterFire
      future: Firebase.initializeApp(
        options: DefaultFirebaseOptions.currentPlatform,
      ),
      builder: (context, snapshot) {
        // Check for errors
        if (snapshot.hasError) {
          return const Center(child: Text("Couldnt connect to FireBase"));
        }

        // Once complete, show your application
        if (snapshot.connectionState == ConnectionState.done) {
          return MaterialApp(
            title: 'Flutter Demo',
            theme: ThemeData(
              primarySwatch: Colors.blue,
            ),
            home: const MyHomePage(),
          );
        }
        // Otherwise, show something whilst waiting for initialization to complete
        return Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: const [
              CircularProgressIndicator(),
              SizedBox(
                height: 15,
              ),
            ],
          ),
        );
      },
    );
  }
}

class MyHomePage extends StatelessWidget {
  const MyHomePage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
        appBar: AppBar(
          title: const Text("MyApp"),
        ),
        body: Center(
            child: Column(
                children: const [Text("Test"), GetUserName("Poodlers")])));
  }
}
