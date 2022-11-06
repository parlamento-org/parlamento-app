import 'dart:developer';

import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:frontend/external_login/create_user.dart';
import 'package:frontend/themes/base_theme.dart';

class LoginPage extends StatefulWidget {
  final String title;
  const LoginPage({super.key, required this.title});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

/// Manages the 'login section' view.
class _LoginPageState extends State<LoginPage> {
  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();

  bool _hidePasswordInput = true;
  bool _isLoggingIn = false;
  RegExp validEmail = RegExp(
      r"^[a-zA-Z0-9.a-zA-Z0-9.!#$%&'*+-/=?^_`{|}~]+@[a-zA-Z0-9]+\.[a-zA-Z]+");

  static final TextEditingController emailController = TextEditingController();
  static final TextEditingController passwordController =
      TextEditingController();

  static final FocusNode emailFocus = FocusNode();
  static final FocusNode passwordFocus = FocusNode();

  @override
  Widget build(BuildContext context) {
    final MediaQueryData queryData = MediaQuery.of(context);

    return Scaffold(
        backgroundColor: baseTheme.backgroundColor,
        body: SingleChildScrollView(
            child: _isLoggingIn
                ? Container(
                    margin: EdgeInsets.only(top: queryData.size.height / 7.5),
                    alignment: Alignment.center,
                    child: const CircularProgressIndicator())
                : Container(
                    margin: EdgeInsets.only(top: queryData.size.height / 7.5),
                    alignment: Alignment.center,
                    child: Container(
                      constraints: const BoxConstraints(maxWidth: 350),
                      child: Column(
                        children: getWidgets(context, queryData),
                      ),
                    ))));
  }

  /// Aggregates all widgets in a list.
  List<Widget> getWidgets(BuildContext context, MediaQueryData queryData) {
    final List<Widget> widgets = [];

    widgets.add(
        Padding(padding: EdgeInsets.only(bottom: queryData.size.height / 70)));
    widgets.add(createTitle(context));
    widgets.add(
        Padding(padding: EdgeInsets.only(bottom: queryData.size.height / 35)));
    widgets.add(createLoginForm(context, queryData));

    return widgets;
  }

  /// Creates the title for the login menu.
  Widget createTitle(BuildContext context) {
    return Column(children: [
      Icon(
        Icons.account_balance_outlined,
        size: 150.0,
        color: baseTheme.colorScheme.primary,
      ),
      smallSeparator,
      Text(
        widget.title,
        style: mainTitleStyle,
        textAlign: TextAlign.center,
      ),
    ]);
  }

  /// Creates the widgets for the user input fields.
  Widget createLoginForm(BuildContext context, MediaQueryData queryData) {
    List<Widget> widgets = [];
    widgets.add(createEmailInput(context));
    widgets.add(
        Padding(padding: EdgeInsets.only(bottom: queryData.size.height / 35)));
    widgets.add(createPasswordInput());
    widgets.add(
        Padding(padding: EdgeInsets.only(bottom: queryData.size.height / 100)));
    widget.title != "Registar"
        ? widgets.add(Align(
            alignment: Alignment.centerLeft, child: createForgotPassword()))
        : null;
    widgets.add(
        Padding(padding: EdgeInsets.only(bottom: queryData.size.height / 35)));

    widgets.add(createLogInButton(context));
    widget.title != "Registar"
        ? widgets.addAll([
            Padding(
                padding: EdgeInsets.only(bottom: queryData.size.height / 100)),
            createRegisterText()
          ])
        : null;

    return Form(
      key: _formKey,
      child: Padding(
          padding: EdgeInsets.only(
              left: queryData.size.width / 45,
              right: queryData.size.width / 45,
              top: queryData.size.height / 40,
              bottom: queryData.size.height / 45),
          child: Column(
            children: widgets,
          )),
    );
  }

  Widget createForgotPassword() {
    return RichText(
        text: TextSpan(
      text: 'Esqueceu-se da password?',
      style: TextStyle(color: baseTheme.primaryColor),
      recognizer: TapGestureRecognizer()..onTap = () {},
    ));
  }

  Widget createRegisterText() {
    return Row(children: [
      RichText(
          text: TextSpan(
        text: 'Ainda não se registou?  ',
        style: TextStyle(color: baseTheme.primaryColorDark),
      )),
      RichText(
          text: TextSpan(
        text: 'Registe-se agora. ',
        style: TextStyle(color: baseTheme.primaryColor),
        recognizer: TapGestureRecognizer()
          ..onTap = () {
            Navigator.of(context).pushReplacement(MaterialPageRoute(
                builder: (context) => const Scaffold(
                      body: Center(
                        child: LoginPage(title: "Registar"),
                      ),
                    )));
          },
      ))
    ]);
  }

  /// Creates the widget for the username input.
  Widget createEmailInput(BuildContext context) {
    return TextFormField(
        autocorrect: false,
        autofocus: true,
        controller: emailController,
        focusNode: emailFocus,
        onFieldSubmitted: (term) {
          emailFocus.unfocus();
          FocusScope.of(context).requestFocus(passwordFocus);
        },
        decoration: getInputDecoration("Email"),
        validator: (String? value) => validateEmail(value));
  }

  /// Creates the widget for the password input.
  Widget createPasswordInput() {
    return TextFormField(
        style: secondaryTextStyle,
        autocorrect: false,
        obscureText: _hidePasswordInput,
        enableInteractiveSelection: !_hidePasswordInput,
        controller: passwordController,
        focusNode: passwordFocus,
        onFieldSubmitted: (term) {
          passwordFocus.unfocus();
          if (widget.title == "Registar") {
            _register();
          } else {
            _login();
          }
        },
        decoration: passwordFieldDecoration(),
        validator: (String? value) =>
            (value == null || value.isEmpty) ? 'Password is required' : null);
  }

  /// Creates the widget for the user to confirm the inputted login info
  Widget createLogInButton(BuildContext context) {
    return SizedBox(
      child: ElevatedButton(
        style: initialLoginbuttonStyle(baseTheme.primaryColor),
        child: Text(widget.title,
            style: const TextStyle(fontWeight: FontWeight.w400, fontSize: 20),
            textAlign: TextAlign.center),
        onPressed: () {
          if (widget.title == "Registar") {
            _register();
          } else {
            _login();
          }
        },
      ),
    );
  }

  /// Decoration for the password field.
  InputDecoration passwordFieldDecoration() {
    final genericDecoration = getInputDecoration("Password");
    return InputDecoration(
        filled: true,
        fillColor: baseTheme.primaryColorLight,
        hintText: genericDecoration.hintText,
        enabledBorder: genericDecoration.enabledBorder,
        border: genericDecoration.border,
        focusedBorder: genericDecoration.focusedBorder,
        suffixIcon: IconButton(
          icon: Icon(
            _hidePasswordInput ? Icons.visibility : Icons.visibility_off,
          ),
          onPressed: _toggleHidePasswordInput,
        ));
  }

  /// Makes the password input view hidden.
  void _toggleHidePasswordInput() {
    setState(() {
      _hidePasswordInput = !_hidePasswordInput;
    });
  }

  /// Handles the login.
  void _login() {
    final email = emailController.text.trim();
    final password = passwordController.text.trim();

    if (_formKey.currentState!.validate()) {
      setState(() {
        _isLoggingIn = true;
      });
      FirebaseAuth.instance
          .signInWithEmailAndPassword(email: email, password: password)
          .then((value) =>
              Navigator.of(context).pushReplacement(MaterialPageRoute(
                  builder: (context) => const Scaffold(
                        body: Center(
                          child: Text("Login with email and password!"),
                        ),
                      ))))
          .catchError((error) {
        setState(() {
          _isLoggingIn = false;
        });
        if (error?.code == 'user-not-found') {
          log('No user found for that email.');
        } else if (error?.code == 'wrong-password') {
          log('Wrong password provided for that user.');
        }
      });
    }
  }

  /// handles register
  void _register() {
    final email = emailController.text.trim();
    final password = passwordController.text.trim();

    if (_formKey.currentState!.validate()) {
      setState(() {
        _isLoggingIn = true;
      });
      FirebaseAuth.instance
          .createUserWithEmailAndPassword(email: email, password: password)
          .then((credential) {
        createUser(credential.user!);
        Navigator.of(context).pushReplacement(MaterialPageRoute(
            builder: (context) => const Scaffold(
                  body: Center(
                    child: Text("Registered with email and password!"),
                  ),
                )));
      }).catchError((error) {
        setState(() {
          _isLoggingIn = false;
        });
        if (error.code == 'weak-password') {
          log('The password provided is too weak.');
        } else if (error.code == 'email-already-in-use') {
          log('The account already exists for that email.');
        }
      });
    }
  }

  /// Validates the username input.
  String? validateEmail(String? username) {
    if (username == null || username.isEmpty) {
      return 'Email is required';
    } else if (!username.contains('@') && !validEmail.hasMatch(username)) {
      return 'Invalid email';
    }

    return null;
  }
}
