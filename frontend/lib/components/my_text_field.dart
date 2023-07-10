import 'package:flutter/material.dart';

class MyTextField extends StatefulWidget {
  final String hintText;
  final bool obscureText;
  String currentTextValue = '';

  MyTextField({
    super.key,
    required this.hintText,
    required this.obscureText,
  });

  @override
  State<StatefulWidget> createState() => _MyTextFieldState();
}

class _MyTextFieldState extends State<MyTextField> {
  late TextEditingController _controller;

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 25.0),
      child: TextField(
        controller: _controller,
        obscureText: widget.obscureText,
        onTapOutside: (value) {
          widget.currentTextValue = _controller.text;
        },
        decoration: InputDecoration(
            enabledBorder: const OutlineInputBorder(
              borderSide: BorderSide(color: Colors.white),
            ),
            focusedBorder: OutlineInputBorder(
              borderSide: BorderSide(color: Colors.grey.shade100),
            ),
            fillColor: Colors.white,
            filled: true,
            hintText: widget.hintText,
            hintStyle: TextStyle(color: Colors.grey[500])),
      ),
    );
  }
}
