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
    fontFamily: 'Inter',
  ),
  iconTheme: const IconThemeData(color: Color.fromARGB(255, 255, 255, 255)),
);

const BorderRadius dialogRadius = BorderRadius.all(Radius.circular(4));
