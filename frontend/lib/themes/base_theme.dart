import 'package:flutter/material.dart';

final baseTheme = ThemeData(
  scaffoldBackgroundColor: const Color(0xff6404C5),
  dialogBackgroundColor: const Color.fromARGB(255, 255, 255, 255),
  backgroundColor: const Color(0xffD7D7FF),
  colorScheme: ColorScheme.fromSwatch().copyWith(
    primary: const Color(0xff6404C5),
    secondary: const Color(0xffD7D7FF),
  ),
  textTheme: Typography.blackCupertino.apply(
    bodyColor: const Color.fromARGB(255, 72, 72, 72),
    displayColor: const Color.fromARGB(255, 20, 21, 22),
    fontFamily: 'Inter',
  ),
  iconTheme: const IconThemeData(color: Color.fromARGB(255, 255, 255, 255)),
);

const BorderRadius dialogRadius = BorderRadius.all(Radius.circular(4));
