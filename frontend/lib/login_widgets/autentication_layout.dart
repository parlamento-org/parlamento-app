import 'package:flutter/material.dart';

class AuthenticationLayout extends StatelessWidget {
  /// The main title to show in the top left of the view
  final String title;

  /// The text to show under the subtitle
  final String subtitle;

  /// The text to show in the main CTA button on the view
  final String mainButtonTitle;

  /// The form to show in the middle of the layout
  final Widget form;

  /// Indicates if we want to display the terms text
  final bool showTermsText;

  /// Called when the main button is pressed
  final Function onMainButtonTapped;

  // Called when the user taps on the "Create Account text"
  final Function? onCreateAccountTapped;

  /// Called when tapping on the "Forgot Password" text
  final Function? onForgetPasswordTapped;

  /// Called when the on screen back button is tapped
  final Function? onBackPressed;

  /// The validation message to show on the form
  final String? validationMessage;

  /// Indicates if the form is busy or not
  final bool busy;

  const AuthenticationLayout({
    required this.title,
    required this.subtitle,
    required this.form,
    required this.onMainButtonTapped,
    this.validationMessage,
    this.onCreateAccountTapped,
    this.onForgetPasswordTapped,
    this.onBackPressed,
    this.mainButtonTitle = 'CONTINUE',
    this.showTermsText = false,
    this.busy = false,
    Key? key,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Padding(
        padding: const EdgeInsets.symmetric(horizontal: 25),
        child: ListView(
          children: [],
        ));
  }
}
