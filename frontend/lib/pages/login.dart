import 'package:flutter/material.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/pages/main_page.dart';
import 'package:frontend/pages/register_page.dart';
import 'package:provider/provider.dart';

import '../components/my_button.dart';
import '../components/my_text_field.dart';
import '../themes/base_theme.dart';

/// Validates the username input.
String? validateUsername(String? username) {
  RegExp validEmail = RegExp(
    r"^[a-zA-Z0-9.a-zA-Z0-9.!#$%&'*+-/=?^_`{|}~]+@[a-zA-Z0-9]+\.[a-zA-Z]+",
  );
  if (username == null || username.isEmpty) {
    return 'Username is required';
  } else if (username.contains('@') && !validEmail.hasMatch(username)) {
    return 'Invalid email';
  }

  return null;
}

/// Validates the password input.
String? validatePassword(String? password) {
  if (password == null || password.isEmpty) {
    return 'Password is required';
  }

  return null;
}

enum LoginType { google, facebook, email }

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final TextEditingController usernameController = TextEditingController();

  final TextEditingController passwordController = TextEditingController();

  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  final FocusNode usernameFocus = FocusNode();
  final FocusNode passwordFocus = FocusNode();

  bool _isLoggingIn = false;

  @override
  void dispose() {
    usernameController.dispose();
    passwordController.dispose();
    usernameFocus.dispose();
    passwordFocus.dispose();
    super.dispose();
  }

  Future<void> handleLogIn(LoginType loginType) async {
    if (loginType == LoginType.email && !_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _isLoggingIn = true;
    });

    try {
      final authController = context.read<AuthController>();
      switch (loginType) {
        case LoginType.google:
          await authController.googleSignIn();
          break;
        case LoginType.facebook:
          await authController.facebookSignIn();
          break;
        case LoginType.email:
          final username = usernameController.text.trim();
          final password = passwordController.text.trim();
          await authController.login(username, password);
          break;
      }

      if (!mounted) return;
      setState(() {
        _isLoggingIn = false;
      });
      Navigator.of(
        context,
      ).push(MaterialPageRoute(builder: (context) => const MainPage()));
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _isLoggingIn = false;
      });

      usernameController.text = '';
      passwordController.text = '';
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(error.toString()),
          duration: const Duration(seconds: 2),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final MediaQueryData queryData = MediaQuery.of(context);

    return Scaffold(
      backgroundColor: baseTheme.colorScheme.surface,
      body: SingleChildScrollView(
        child:
            _isLoggingIn
                ? Container(
                  margin: EdgeInsets.only(top: queryData.size.height / 7.5),
                  alignment: Alignment.center,
                  child: const CircularProgressIndicator(),
                )
                : Container(child: buildLoginPage(context)),
      ),
    );
  }

  Widget buildLoginPage(BuildContext context) {
    return SafeArea(
      child: Form(
        key: _formKey,
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const SizedBox(height: 50),

            // logo
            Image.asset('lib/images/logo.png', height: 120, width: 120),

            const SizedBox(height: 30),

            // welcome back, you've been missed!
            Text(
              'Bem vinde!',
              style: TextStyle(
                color: baseTheme.colorScheme.primary,
                fontSize: 30,
              ),
            ),

            const SizedBox(height: 25),

            // username textfield
            MyTextField(
              hintText: 'Username',
              obscureText: false,
              validateInput: validateUsername,
              focusNode: usernameFocus,
              controller: usernameController,
            ),

            const SizedBox(height: 10),

            // password textfield
            MyTextField(
              hintText: 'Password',
              obscureText: true,
              validateInput: validatePassword,
              focusNode: passwordFocus,
              controller: passwordController,
            ),

            const SizedBox(height: 10),

            // forgot password?
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 25.0),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  Text(
                    'Forgot Password?',
                    style: TextStyle(color: baseTheme.colorScheme.primary),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 25),

            // sign in button
            MyButton(
              text: 'Sign In',
              onTap: () => handleLogIn(LoginType.email),
            ),

            const SizedBox(height: 20),

            // or continue with
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 25.0),
              child: Row(
                children: [
                  Expanded(
                    child: Divider(
                      thickness: 0.5,
                      color: baseTheme.colorScheme.primary,
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 10.0),
                    child: Text(
                      'Or continue with',
                      style: TextStyle(color: baseTheme.colorScheme.primary),
                    ),
                  ),
                  Expanded(
                    child: Divider(thickness: 0.5, color: Colors.grey[400]),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 50),

            // google + apple sign in buttons
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                IconButton(
                  tooltip: 'Google',
                  onPressed: () => handleLogIn(LoginType.google),
                  icon: Image.asset(
                    'lib/images/google.png',
                    width: 50,
                    height: 50,
                  ),
                ),

                const SizedBox(width: 25),
                IconButton(
                  tooltip: 'Facebook',
                  onPressed: () => handleLogIn(LoginType.facebook),
                  icon: Image.asset(
                    'lib/images/facebook.png',
                    width: 45,
                    height: 45,
                  ),
                ),
              ],
            ),

            const SizedBox(height: 10),

            // not a member? register now
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  'Not a member?',
                  style: TextStyle(color: baseTheme.colorScheme.primary),
                ),
                const SizedBox(width: 4),
                TextButton(
                  child: Text(
                    'Register now',
                    style: TextStyle(
                      color: baseTheme.colorScheme.primary,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  onPressed:
                      () => Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (context) => const RegisterPage(),
                        ),
                      ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
