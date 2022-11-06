import 'package:flutter/material.dart';

final baseTheme = ThemeData(
  primaryColor: const Color(0xff6404C5),
  primaryColorLight: const Color.fromARGB(255, 255, 255, 255),
  primaryColorDark: const Color.fromARGB(255, 72, 72, 72),
  scaffoldBackgroundColor: const Color(0xff6404C5),
  dialogBackgroundColor: const Color.fromARGB(255, 255, 255, 255),
  backgroundColor: const Color(0xffD7D7FF),
  colorScheme: ColorScheme.fromSwatch().copyWith(
      primary: const Color(0xff6404C5),
      secondary: const Color(0xffD7D7FF),
      tertiary: const Color.fromARGB(255, 20, 21, 22)),
  textTheme: Typography.blackCupertino.apply(
    bodyColor: const Color.fromARGB(255, 72, 72, 72),
    displayColor: const Color.fromARGB(255, 20, 21, 22),
    fontFamily: 'Inter',
  ),
  iconTheme: const IconThemeData(color: Color.fromARGB(255, 255, 255, 255)),
);

const BorderRadius dialogRadius = BorderRadius.all(Radius.circular(4));
TextStyle mainTitleStyle = TextStyle(
  color: baseTheme.primaryColor,
  fontFamily: 'Inter',
  fontSize: 45.0,
  fontWeight: FontWeight.bold,
);

TextStyle secondaryTextStyle = TextStyle(
  color: baseTheme.colorScheme.tertiary,
  fontFamily: 'Inter',
  fontSize: 16.0,
);

const smallSeparator = SizedBox(
  height: 20,
);

const BorderRadius inputRadius = BorderRadius.all(Radius.circular(15.0));

InputDecoration getInputDecoration(String hintText) {
  return InputDecoration(
      filled: true,
      fillColor: baseTheme.primaryColorLight,
      hoverColor: baseTheme.primaryColorLight,
      hintText: hintText,
      enabledBorder: OutlineInputBorder(
          borderRadius: inputRadius,
          borderSide: BorderSide(color: baseTheme.primaryColorDark)),
      border: const OutlineInputBorder(borderRadius: inputRadius),
      focusedBorder: const OutlineInputBorder(
          borderSide: BorderSide(width: 2), borderRadius: inputRadius));
}

ButtonStyle initialLoginbuttonStyle(Color backgroundColor) => ButtonStyle(
    textStyle: MaterialStateProperty.resolveWith(((states) {
      return secondaryTextStyle;
    })),
    minimumSize: MaterialStateProperty.resolveWith(
        (states) => const Size.fromHeight(50)),
    backgroundColor:
        MaterialStateProperty.resolveWith((states) => backgroundColor),
    shape: MaterialStateProperty.resolveWith((states) =>
        const RoundedRectangleBorder(
            borderRadius: BorderRadius.all(Radius.circular(10.0)))));
