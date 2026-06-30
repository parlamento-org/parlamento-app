import 'package:flutter/material.dart';
import 'package:frontend/themes/base_theme.dart';

class MyTextField extends StatelessWidget {
  final String hintText;
  final bool obscureText;
  final String? Function(String?)? validateInput;
  final FocusNode? focusNode;
  final TextEditingController controller;

  const MyTextField({
    super.key,
    required this.hintText,
    required this.obscureText,
    required this.controller,
    this.validateInput,
    this.focusNode,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 25.0),
      child: TextFormField(
        controller: controller,
        obscureText: obscureText,
        validator: validateInput,
        focusNode: focusNode,
        autocorrect: false,
        decoration: InputDecoration(
          enabledBorder: const OutlineInputBorder(
            borderSide: BorderSide(color: Colors.white),
          ),
          focusedBorder: OutlineInputBorder(
            borderSide: BorderSide(
              color: baseTheme.colorScheme.secondary,
              width: 3.0,
            ),
          ),
          fillColor: Colors.white,
          filled: true,
          hintText: hintText,
          hintStyle: TextStyle(color: Colors.grey[500]),
        ),
      ),
    );
  }
}
