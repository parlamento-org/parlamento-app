import 'package:flutter/material.dart';

class VotePage extends StatefulWidget {
  const VotePage({super.key});

  @override
  State<VotePage> createState() => _VotePageState();
}

class _VotePageState extends State<VotePage> {
  @override
  Widget build(BuildContext context) {
    return Padding(
        padding: EdgeInsets.all(50),
        child:
            Container(alignment: Alignment.center, child: Text('Vote Page')));
  }
}
