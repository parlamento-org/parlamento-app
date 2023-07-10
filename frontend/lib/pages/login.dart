import 'package:flutter/material.dart';
import 'package:frontend/constants/user_session.dart';
import 'package:frontend/controllers/user_controller.dart';
import 'package:frontend/models/user.dart';

import '../components/my_button.dart';
import '../components/my_text_field.dart';
import '../components/squareTile.dart';
import '../exceptions/InvalidCredentials.dart';
import '../themes/base_theme.dart';

class Login extends StatelessWidget {
  Login({super.key});

  final UserController _userController = UserController();
  final MyTextField _usernameTextField = MyTextField(
    hintText: 'Username',
    obscureText: false,
  );
  final MyTextField _passwordTextField = MyTextField(
    hintText: 'Password',
    obscureText: true,
  );
  // sign user in method
  void signUserIn() async {
    String username = _usernameTextField.currentTextValue;
    String password = _passwordTextField.currentTextValue;
    try {
      UserSession user = await _userController.login(username, password);
      globalUserSession = user;

      print('User logged in successfully!');
      //redirect to home page
    } catch (e) {
      if (e is InvalidCredentials) {
        print("Please provide a correct username and password!");
      } else {
        print("Something went wrong!");
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: baseTheme.colorScheme.background,
      body: SafeArea(
        child: Center(
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
                onTap: signUserIn,
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
              const Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  // google button
                  SquareTile(imagePath: 'lib/images/google.png'),
                  SizedBox(width: 25),
                  // apple button
                  SquareTile(imagePath: 'lib/images/facebook.png')
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
                  Text(
                    'Register now',
                    style: TextStyle(
                      color: baseTheme.colorScheme.primary,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ],
              )
            ],
          ),
        ),
      ),
    );
  }
}
