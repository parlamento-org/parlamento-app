import 'dart:developer';

import 'package:cloud_firestore/cloud_firestore.dart';
import 'package:flutter/material.dart';

class GetUserName extends StatelessWidget {
  final String userName;

  const GetUserName(this.userName, {super.key});

  @override
  Widget build(BuildContext context) {
    CollectionReference users = FirebaseFirestore.instance.collection('users');

    return FutureBuilder<QuerySnapshot>(
      future: users.where('username', isEqualTo: userName).get(),
      builder: (BuildContext context, AsyncSnapshot<QuerySnapshot> snapshot) {
        if (snapshot.hasError) {
          log(snapshot.error.toString());
          return const Text("Something went wrong");
        }
        log(snapshot.data!.docs.length.toString());
        if (snapshot.hasData && snapshot.data!.docs.isEmpty) {
          return const Text("Document does not exist");
        }

        if (snapshot.connectionState == ConnectionState.done) {
          return ListView(
            children: snapshot.data!.docs.map((DocumentSnapshot document) {
              Map<String, dynamic> data =
                  document.data()! as Map<String, dynamic>;
              return ListTile(
                title: Text(data['username']),
                subtitle: Text(data['email']),
              );
            }).toList(),
          );
        }

        return const Text("loading");
      },
    );
  }
}
