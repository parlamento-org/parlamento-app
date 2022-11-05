import 'package:flutter/material.dart';

import '../themes/base_theme.dart';

class LoginMethodButton extends StatelessWidget {
  /// The main title to show in the top left of the view
  final void Function() onPressed;

  final String buttonText;
  final IconData buttonIcon;
  const LoginMethodButton(
      {Key? key,
      required this.onPressed,
      required this.buttonText,
      required this.buttonIcon})
      : super(key: key);

  @override
  Widget build(BuildContext context) {
    return ElevatedButton(
        style: initialLoginbuttonStyle,
        onPressed: onPressed,
        child: Padding(
          padding: const EdgeInsets.all(5.0),
          child: Stack(
            children: <Widget>[
              Align(
                  alignment: Alignment.centerLeft,
                  child: Icon(
                    buttonIcon,
                    color: baseTheme.primaryColor,
                    size: 24.0,
                  )),
              Align(
                  alignment: Alignment.center,
                  child: Text(buttonText, style: secondaryTextStyle))
            ],
          ), // <-- Text
        ));
  }
}
