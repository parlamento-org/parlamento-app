import 'package:flutter/material.dart';
import 'package:frontend/constants/user_session.dart';
import 'package:frontend/controllers/user_controller.dart';
import 'package:frontend/models/user.dart';
import 'package:frontend/pages/main_page.dart';
import 'package:frontend/pages/register_page.dart';

import '../components/my_button.dart';
import '../components/my_text_field.dart';
import '../themes/base_theme.dart';

/// Validates the username input.
String? validateUsername(String? username) {
  RegExp validEmail = RegExp(
      r"^[a-zA-Z0-9.a-zA-Z0-9.!#$%&'*+-/=?^_`{|}~]+@[a-zA-Z0-9]+\.[a-zA-Z]+");
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
  final UserController _userController = UserController();
  static final TextEditingController usernameController =
      TextEditingController();

  static final TextEditingController passwordController =
      TextEditingController();

  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  static final FocusNode usernameFocus = FocusNode();
  static final FocusNode passwordFocus = FocusNode();

  bool _isLoggingIn = false;

  final MyTextField _usernameTextField = MyTextField(
    hintText: 'Username',
    obscureText: false,
    validateInput: validateUsername,
    focusNode: usernameFocus,
    controller: usernameController,
  );
  final MyTextField _passwordTextField = MyTextField(
    hintText: 'Password',
    obscureText: true,
    validateInput: validatePassword,
    focusNode: passwordFocus,
    controller: passwordController,
  );
  // sign user in method

  void handleLogIn(LoginType loginType, BuildContext context) async {
    setState(() {
      _isLoggingIn = true;
    });
    Future<UserSession> signInRequest;
    switch (loginType) {
      case LoginType.google:
        signInRequest = _userController.googleSignIn();
        break;
      case LoginType.facebook:
        signInRequest = _userController.facebookSignIn();
        break;
      case LoginType.email:
        final username = usernameController.text.trim();
        final password = passwordController.text.trim();
        signInRequest = _userController.login(username, password);
        break;
    }
    signInRequest.then((userSession) {
      setState(() {
        _isLoggingIn = false;
      });
      globalUserSession = userSession;
      Navigator.of(context)
          .push(MaterialPageRoute(builder: (context) => MainPage()));
    }).catchError((error) {
      setState(() {
        _isLoggingIn = false;
      });
      usernameController.text = '';
      passwordController.text = '';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(error.toString()),
        duration: const Duration(seconds: 2),
      ));
    });
  }

  @override
  Widget build(BuildContext context) {
    final MediaQueryData queryData = MediaQuery.of(context);

    return Scaffold(
        backgroundColor: baseTheme.colorScheme.background,
        body: SingleChildScrollView(
            child: _isLoggingIn
                ? Container(
                    margin: EdgeInsets.only(top: queryData.size.height / 7.5),
                    alignment: Alignment.center,
                    child: const CircularProgressIndicator())
                : Container(
                    child: buildLoginPage(context),
                  )));
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
            Image.asset(
              'lib/images/logo.png',
              height: 120,
              width: 120,
            ),

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
            _usernameTextField,

            const SizedBox(height: 10),

            // password textfield
            _passwordTextField,

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
                onTap: () => handleLogIn(LoginType.email, context)),

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
                    child: Divider(
                      thickness: 0.5,
                      color: Colors.grey[400],
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 50),

            // google + apple sign in buttons
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                // google button
                GestureDetector(
                  onTap: () => handleLogIn(LoginType.google, context),
                  child: Image.asset(
                    'lib/images/google.png',
                    width: 50,
                    height: 50,
                  ),
                ),

                const SizedBox(width: 25),
                // apple button
                GestureDetector(
                  onTap: () => handleLogIn(LoginType.facebook, context),
                  child: Image.asset(
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
                  onPressed: () => Navigator.of(context).push(MaterialPageRoute(
                      builder: (context) => const RegisterPage())),
                ),
              ],
            )
          ],
        ),
      ),
    );
  }
}
