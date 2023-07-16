import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:frontend/exceptions/email_has_account.dart';
import 'package:frontend/exceptions/invalid_credentials.dart';
import 'package:frontend/exceptions/username_already_exists.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/proposal_criteria.dart';
import 'package:frontend/models/user.dart';
import 'dart:convert';
import 'package:crypto/crypto.dart';
import 'package:frontend/models/vote_model.dart';
import 'package:http/http.dart' as http;

import '../models/proposal.dart';

class APIRepository implements Repository {
  String api_url = dotenv.env['BACKEND_URL']!;

  @override
  Future<Proposal> getProposal(ProposalCriteria criteria) async {
    final url = Uri.parse('$api_url/vote');

    try {
      print(criteria.toJson());
      final response =
          await http.put(url, body: jsonEncode(criteria.toJson()), headers: {
        'Content-Type': 'application/json',
      });

      // Check the response status code
      if (response.statusCode == 200) {
        print('Response body: ${response.body}');
        final jsonResponse = jsonDecode(response.body);
        final proposal = Proposal.fromJson(jsonResponse);
        return proposal;
      } else {
        throw Exception('Failed to load proposal');
      }
    } catch (e) {
      rethrow;
    }
  }

  @override
  Future<UserSession> facebookSignInRequest(
      String idToken, String email, String name, int profilePicId) async {
    final url = Uri.parse('$api_url/fb-login');

    // Create a Map object containing the data to be sent in the request body
    final Map<String, dynamic> data = {
      "facebookIDToken": idToken,
      "email": email,
      "userName": name,
      "profilePic": profilePicId
    };

    // Convert the data to JSON format
    final jsonData = jsonEncode(data);

    try {
      final response = await http.post(url, body: jsonData, headers: {
        'Content-Type': 'application/json',
      });

      // Check the response status code
      if (response.statusCode == 200) {
        print('Response body: ${response.body}');
        final jsonResponse = jsonDecode(response.body);
        final user = UserSession.fromJson(jsonResponse);
        return user;
      } else {
        throw InvalidCredentials();
      }
    } catch (e) {
      rethrow;
    }
  }

  @override
  Future<UserSession> googleSignInRequest(
      String idToken, String email, String name, int profilePicId) async {
    final url = Uri.parse('$api_url/google-login');

    // Create a Map object containing the data to be sent in the request body
    final Map<String, dynamic> data = {
      "googleIDToken": idToken,
      "email": email,
      "userName": name,
      "profilePic": profilePicId
    };

    // Convert the data to JSON format
    final jsonData = jsonEncode(data);

    try {
      final response = await http.post(url, body: jsonData, headers: {
        'Content-Type': 'application/json',
      });

      // Check the response status code
      if (response.statusCode == 200) {
        print('Response body: ${response.body}');
        final jsonResponse = jsonDecode(response.body);
        final user = UserSession.fromJson(jsonResponse);
        return user;
      } else {
        throw InvalidCredentials();
      }
    } catch (e) {
      rethrow;
    }
  }

  @override
  Future<UserSession> loginRequest(String email, String password) async {
    final url = Uri.parse('$api_url/user-login');

    //encode password
    final passwordBytes = utf8.encode(password);
    final passwordHash = sha256.convert(passwordBytes);
    final encodedPassword = passwordHash.toString();
    var identifier = 'userName';
    //determine if email or username
    if (email.contains('@')) {
      identifier = 'email';
    }

    // Create a Map object containing the data to be sent in the request body
    final Map<String, dynamic> data = {
      identifier: email,
      "password": encodedPassword
    };

    // Convert the data to JSON format
    final jsonData = jsonEncode(data);

    try {
      final response = await http.post(url, body: jsonData, headers: {
        'Content-Type': 'application/json',
      });

      // Check the response status code
      if (response.statusCode == 200) {
        print('Response body: ${response.body}');
        final jsonResponse = jsonDecode(response.body);
        final user = UserSession.fromJson(jsonResponse);
        return user;
      } else {
        throw InvalidCredentials();
      }
    } catch (e) {
      rethrow;
    }
  }

  @override
  Future<bool> registerRequest(
      String email, String userName, String password, int profilePicId) async {
    final url = Uri.parse('$api_url/user');

    //encode password LATER
    final passwordBytes = utf8.encode(password);
    final passwordHash = sha256.convert(passwordBytes);
    final encodedPassword = passwordHash.toString();

    // Create a Map object containing the data to be sent in the request body
    final Map<String, dynamic> data = {
      "email": email,
      "userName": userName,
      "password": encodedPassword,
      "profilePic": profilePicId
    };

    // Convert the data to JSON format
    final jsonData = jsonEncode(data);

    try {
      final response = await http.post(url, body: jsonData, headers: {
        'Content-Type': 'application/json',
      });

      // Check the response status code
      if (response.statusCode == 200) {
        print('Post request successful!');
        print('Response body: ${response.body}');
        return true;
      } else if (response.statusCode == 401) {
        throw EmailHasAccount();
      } else if (response.statusCode == 402) {
        throw UsernameAlreadyExists();
      }
      return false;
    } catch (e) {
      rethrow;
    }
  }

  @override
  Future<void> castUserVote(UserVote userVote) async {
    final url = Uri.parse('$api_url/vote');

    // Create a Map object containing the data to be sent in the request body
    final Map<String, dynamic> data = userVote.toJson();

    // Convert the data to JSON format
    final jsonData = jsonEncode(data);

    try {
      final response = await http.post(url, body: jsonData, headers: {
        'Content-Type': 'application/json',
      });

      // Check the response status code
      if (response.statusCode == 200) {
        print('Post request successful!');
        print('Response body: ${response.body}');
        return;
      } else {
        throw Exception('Failed to cast vote');
      }
    } catch (e) {
      rethrow;
    }
  }
}
