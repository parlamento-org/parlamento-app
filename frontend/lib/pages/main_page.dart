import 'package:flutter/material.dart';
import 'package:frontend/constants/user_session.dart';

class MainPage extends StatelessWidget {
  const MainPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Text('Hello, World! I am ${globalUserSession.name}');
  }
}
