import 'dart:developer';

import 'package:cloud_firestore/cloud_firestore.dart';
import 'package:firebase_auth/firebase_auth.dart';

void createUser(User user) {
  log(user.toString());
  CollectionReference users = FirebaseFirestore.instance.collection('users');
  users.where('id', isEqualTo: user.uid).get().then((snapshot) {
    if (snapshot.size == 0) {
      users
          .add({
            'id': user.uid,
            'username': user.displayName,
            'email': user.email,
            'partidos': {
              'BE': 0,
              'CH': 0,
              'IL': 0,
              'L': 0,
              'PAN': 0,
              'PCP': 0,
              'PS': 0,
              'PSD': 0
            },
            'votacoes': []
          })
          .then((value) => log("User Added"))
          .catchError((error) => log("Failed to add user: $error"));
    } else {
      log("User already in database");
    }
  });
}
