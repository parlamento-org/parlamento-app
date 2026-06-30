import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:frontend/constants/user_session.dart';
import 'package:frontend/exceptions/google_sign_in_error.dart';
import 'package:frontend/fetcher/api_repository.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/user.dart';
import 'package:google_sign_in/google_sign_in.dart';
import 'package:flutter_facebook_auth/flutter_facebook_auth.dart';

class UserController {
  final Repository _repository = APIRepository();
  static Future<void>? _googleSignInInitialization;

  Future<void> _initializeGoogleSignIn() {
    return _googleSignInInitialization ??= GoogleSignIn.instance.initialize(
      clientId: dotenv.env['GOOGLE_CLIENT_ID'],
    );
  }

  Future<void> logout() async {
    if (globalUserSession.userType == UserType.google) {
      await _initializeGoogleSignIn();
      await GoogleSignIn.instance.signOut();
    } else if (globalUserSession.userType == UserType.facebook) {
      await FacebookAuth.instance.logOut();
    }
    globalUserSession = UserSession.empty();
  }

  Future<UserSession> facebookSignIn() async {
    try {
      await FacebookAuth.instance.login();
      //final AccessToken accessToken = result.accessToken!;
      final userData = await FacebookAuth.instance.getUserData();

      const profilePic = 0;
      final user = await _repository.facebookSignInRequest(
        userData['id'],
        userData['email'],
        userData['name'],
        profilePic,
      );
      return user;
    } catch (error) {
      rethrow;
    }
  }

  Future<UserSession> googleSignIn() async {
    try {
      await _initializeGoogleSignIn();
      if (!GoogleSignIn.instance.supportsAuthenticate()) {
        throw GoogleSignInError();
      }
      final GoogleSignInAccount googleSignInAccount = await GoogleSignIn
          .instance
          .authenticate(scopeHint: const ['email']);

      final email = googleSignInAccount.email;
      final name = googleSignInAccount.displayName;
      const profilePic = 0;
      final idToken = googleSignInAccount.authentication.idToken;

      if (idToken == null || name == null) {
        throw GoogleSignInError();
      }

      final user = await _repository.googleSignInRequest(
        idToken,
        email,
        name,
        profilePic,
      );
      return user;
    } catch (error) {
      throw GoogleSignInError();
    }
  }

  Future<UserSession> login(String email, String password) async {
    try {
      final user = await _repository.loginRequest(email, password);

      return user;
    } catch (e) {
      rethrow;
    }
  }

  Future<bool> register(
    String email,
    String userName,
    String password,
    int profilePicID,
  ) async {
    try {
      final registerSucess = await _repository.registerRequest(
        email,
        userName,
        password,
        profilePicID,
      );

      return registerSucess;
    } catch (e) {
      rethrow;
    }
  }
}
