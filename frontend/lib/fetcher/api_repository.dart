import 'package:frontend/exceptions/email_has_account.dart';
import 'package:frontend/exceptions/invalid_credentials.dart';
import 'package:frontend/exceptions/username_already_exists.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/user.dart';
import 'dart:convert';
import 'package:crypto/crypto.dart';
import 'package:http/http.dart' as http;
import '../constants/api_endpoints.dart';

class APIRepository implements Repository {
  @override
  Future<UserSession> googleSignInRequest(
      String idToken, String email, String name, int profilePicId) async {
    final url = Uri.parse('$localAPIURL/google-login');

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
    final url = Uri.parse('$localAPIURL/user-login');

    //encode password
    final passwordBytes = utf8.encode(password);
    final passwordHash = sha256.convert(passwordBytes);
    final encodedPassword = passwordHash.toString();

    // Create a Map object containing the data to be sent in the request body
    final Map<String, dynamic> data = {
      "email": email,
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
    final url = Uri.parse('$localAPIURL/user');

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
}
