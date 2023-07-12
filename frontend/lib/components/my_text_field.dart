import 'package:flutter/material.dart';
import 'package:frontend/themes/base_theme.dart';

class MyTextField extends StatefulWidget {
  final String hintText;
  final bool obscureText;
  String currentTextValue = '';
  String? Function(String?)? validateInput;
  FocusNode? focusNode;
  final TextEditingController controller;

  MyTextField({
    super.key,
    required this.hintText,
    required this.obscureText,
    required this.controller,
    this.validateInput,
    this.focusNode,
  });

  @override
  State<StatefulWidget> createState() => _MyTextFieldState();
}

class _MyTextFieldState extends State<MyTextField> {
  @override
  void initState() {
    super.initState();
  }

  @override
  void dispose() {
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 25.0),
      child: TextFormField(
        controller: widget.controller,
        obscureText: widget.obscureText,
        onTapOutside: (value) {
          widget.currentTextValue = widget.controller.text;
        },
        validator: widget.validateInput,
        focusNode: widget.focusNode,
        autocorrect: false,
        decoration: InputDecoration(
            enabledBorder: const OutlineInputBorder(
              borderSide: BorderSide(color: Colors.white),
            ),
            focusedBorder: OutlineInputBorder(
              borderSide: BorderSide(
                  color: baseTheme.colorScheme.secondary, width: 3.0),
            ),
            fillColor: Colors.white,
            filled: true,
            hintText: widget.hintText,
            hintStyle: TextStyle(color: Colors.grey[500])),
      ),
    );
  }
}
