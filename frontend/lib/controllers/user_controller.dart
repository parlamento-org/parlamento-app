import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:frontend/exceptions/invalid_credentials.dart';
import 'package:frontend/exceptions/google_sign_in_error.dart';
import 'package:frontend/fetcher/api_repository.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/user.dart';
import 'package:google_sign_in/google_sign_in.dart';
import 'package:flutter_facebook_auth/flutter_facebook_auth.dart';

class UserController {
  UserController({Repository? repository})
    : _repository = repository ?? APIRepository();

  final Repository _repository;
  static Future<void>? _googleSignInInitialization;

  static Future<void> initializeGoogleSignIn() {
    return _googleSignInInitialization ??= GoogleSignIn.instance.initialize(
      clientId: dotenv.env['GOOGLE_CLIENT_ID'],
    );
  }

  Future<void> logout(UserType userType) async {
    if (userType == UserType.google) {
      await initializeGoogleSignIn();
      await GoogleSignIn.instance.signOut();
    } else if (userType == UserType.facebook) {
      await FacebookAuth.instance.logOut();
    }
  }

  Future<UserSession> facebookSignIn() async {
    final loginResult = await FacebookAuth.instance.login();
    final accessToken = loginResult.accessToken;
    if (loginResult.status != LoginStatus.success ||
        accessToken == null ||
        accessToken.isExpired) {
      throw InvalidCredentials();
    }

    final userData = await FacebookAuth.instance.getUserData();

    const profilePic = 0;
    return _repository.facebookSignInRequest(
      accessToken.token,
      userData['email'],
      userData['name'],
      profilePic,
    );
  }

  Future<UserSession> googleSignIn() async {
    try {
      await initializeGoogleSignIn();
      if (!GoogleSignIn.instance.supportsAuthenticate()) {
        throw GoogleSignInError();
      }
      final GoogleSignInAccount googleSignInAccount = await GoogleSignIn
          .instance
          .authenticate(scopeHint: const ['email', 'profile']);

      return googleSignInWithAccount(googleSignInAccount);
    } catch (error) {
      throw GoogleSignInError();
    }
  }

  Future<UserSession> googleSignInWithAccount(
    GoogleSignInAccount googleSignInAccount,
  ) async {
    final email = googleSignInAccount.email;
    final name = googleSignInAccount.displayName ?? email;
    const profilePic = 0;
    final idToken = googleSignInAccount.authentication.idToken;

    if (idToken == null) {
      throw GoogleSignInError();
    }

    return _repository.googleSignInRequest(idToken, email, name, profilePic);
  }

  Future<UserSession> login(String email, String password) async {
    return _repository.loginRequest(email, password);
  }

  Future<UserSession> restoreSession() async {
    return _repository.currentSessionRequest();
  }

  Future<bool> register(
    String email,
    String userName,
    String password,
    int profilePicID,
  ) async {
    return _repository.registerRequest(email, userName, password, profilePicID);
  }
}
