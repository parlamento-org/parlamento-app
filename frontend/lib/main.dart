import 'package:flutter/foundation.dart' show defaultTargetPlatform, kIsWeb;
import 'package:flutter/material.dart';
import 'package:flutter_facebook_auth/flutter_facebook_auth.dart';
import 'package:frontend/pages/vote_page.dart';
import 'package:frontend/themes/base_theme.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

import 'pages/login.dart';

void main() async {
  await dotenv.load();

  if (kIsWeb || defaultTargetPlatform == TargetPlatform.macOS) {
    // initialiaze the facebook javascript SDK
    await FacebookAuth.instance.webAndDesktopInitialize(
      appId: dotenv.env['FACEBOOK_APP_ID']!,
      cookie: true,
      xfbml: true,
      version: "v15.0",
    );
  }
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  // This widget is the root of your application.
  @override
  Widget build(BuildContext context) {
    return const MyHomePage();
  }
}

class MyHomePage extends StatelessWidget {
  const MyHomePage({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Parlamento Project',
      theme: baseTheme,
      home: const VotePage(),
    );
  }
}
