import 'package:flutter/material.dart';
import 'package:frontend/controllers/user_controller.dart';
import 'package:frontend/pages/login.dart';

import '../components/my_button.dart';
import '../components/my_text_field.dart';
import '../components/selectable_image.dart';
import 'package:email_validator/email_validator.dart';
import '../themes/base_theme.dart';

/// Validates the username input.
String? validateEmail(String? email) {
  if (email == null || email.isEmpty) {
    return 'Email is required';
  } else {
    final bool isValid = EmailValidator.validate(email);
    if (!isValid) {
      return 'Please enter a valid email';
    }
  }

  return null;
}

String? validateUsername(String? username) {
  if (username == null || username.isEmpty) {
    return 'Username is required';
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

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});

  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final UserController _userController = UserController();
  final TextEditingController usernameController = TextEditingController();

  final TextEditingController emailController = TextEditingController();
  final TextEditingController passwordController = TextEditingController();

  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  final FocusNode usernameFocus = FocusNode();
  final FocusNode passwordFocus = FocusNode();
  final FocusNode emailFocus = FocusNode();

  var _selectedImageIndex = 0;
  final _images = [
    'lib/images/google.png',
    'lib/images/google.png',
    'lib/images/google.png',
  ];

  bool _isSigningUp = false;

  @override
  void dispose() {
    usernameController.dispose();
    emailController.dispose();
    passwordController.dispose();
    usernameFocus.dispose();
    passwordFocus.dispose();
    emailFocus.dispose();
    super.dispose();
  }

  Future<void> signUserUp() async {
    final username = usernameController.text.trim();
    final email = emailController.text.trim();
    final password = passwordController.text.trim();
    final profilePicId = _selectedImageIndex;

    if (_formKey.currentState!.validate()) {
      setState(() {
        _isSigningUp = true;
      });

      try {
        final registerSucess = await _userController.register(
          email,
          username,
          password,
          profilePicId,
        );

        if (!mounted) return;
        setState(() {
          _isSigningUp = false;
        });
        if (registerSucess) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text("User registered successfully!"),
              duration: Duration(seconds: 2),
            ),
          );
        }
        Navigator.of(
          context,
        ).push(MaterialPageRoute(builder: (context) => const LoginPage()));
      } catch (error) {
        if (!mounted) return;
        usernameController.text = '';
        passwordController.text = '';
        emailController.text = '';
        setState(() {
          _isSigningUp = false;
        });

        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(error.toString()),
            duration: const Duration(seconds: 2),
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final MediaQueryData queryData = MediaQuery.of(context);

    return Scaffold(
      backgroundColor: baseTheme.colorScheme.surface,
      body: SingleChildScrollView(
        child:
            _isSigningUp
                ? Container(
                  margin: EdgeInsets.only(top: queryData.size.height / 7.5),
                  alignment: Alignment.center,
                  child: const CircularProgressIndicator(),
                )
                : Container(child: buildRegisterPage(context)),
      ),
    );
  }

  Widget buildRegisterPage(BuildContext context) {
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

            MyTextField(
              hintText: 'Email',
              obscureText: false,
              validateInput: validateEmail,
              focusNode: emailFocus,
              controller: emailController,
            ),

            const SizedBox(height: 10),
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
            Text(
              'Escolhe a tua imagem de perfil',
              style: TextStyle(
                color: baseTheme.colorScheme.primary,
                fontSize: 20,
              ),
            ),

            const SizedBox(height: 10),
            Padding(
              padding: const EdgeInsets.only(left: 40, right: 40),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                children: [
                  for (int i = 0; i < _images.length; i++)
                    SelectableImage(
                      isSelected: _selectedImageIndex == i,
                      onTap: (selectedImageIndex) {
                        setState(() {
                          _selectedImageIndex = i;
                        });
                      },
                      imageAsset: _images[i],
                    ),
                ],
              ),
            ),

            const SizedBox(height: 25),

            // sign in button
            MyButton(text: 'Register', onTap: signUserUp),

            const SizedBox(height: 20),
          ],
        ),
      ),
    );
  }
}
