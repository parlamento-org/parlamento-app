import 'package:flutter/material.dart';

final baseTheme = ThemeData(
  scaffoldBackgroundColor: const Color(0xfff5f5),
  dialogBackgroundColor: const Color(0xfff5f5),
  colorScheme: ColorScheme.fromSwatch().copyWith(
    primary: const Color(0xffb62f0d),
    secondary: const Color(0xffB6830D),
    background: const Color(0xffFFF5F5),
  ),
  textTheme: Typography.blackCupertino.apply(
    bodyColor: const Color.fromARGB(255, 72, 72, 72),
    displayColor: const Color(0xffb62f0d),
    fontFamily: 'Montseratt',
    fontSizeFactor: 1.0,
  ),
  iconTheme: const IconThemeData(color: Color.fromARGB(255, 255, 255, 255)),
);

const BorderRadius dialogRadius = BorderRadius.all(Radius.circular(4));
const approvedGreenBold = Color(0xff14C044);
var approvedGreenNormal = const Color(0xff2ABE4B).withOpacity(0.48);
const rejectedRedBold = Color(0xffEC1C24);
const rejectedRedNormal = Color(0xffFAAEAE);

ButtonStyle buttonStyle = ButtonStyle(
  backgroundColor:
      MaterialStateProperty.all<Color>(baseTheme.colorScheme.primary),
  shape: MaterialStateProperty.all<RoundedRectangleBorder>(
    RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(10.0),
    ),
  ),
);
