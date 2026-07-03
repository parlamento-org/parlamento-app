import 'package:flutter/foundation.dart';
import 'package:frontend/auth/token_storage.dart';
import 'package:frontend/controllers/user_controller.dart';
import 'package:frontend/models/user.dart';
import 'package:google_sign_in/google_sign_in.dart';

class AuthController extends ChangeNotifier {
  AuthController({UserController? userController})
    : _userController = userController ?? UserController() {
    AppTokenStore.onUnauthorized = _handleUnauthorized;
  }

  final UserController _userController;
  UserSession? _session;

  UserSession? get session => _session;
  bool get isLoggedIn => _session?.isLoggedIn ?? false;

  Future<UserSession> login(String email, String password) async {
    final userSession = await _userController.login(email, password);
    await _setSession(userSession);
    return userSession;
  }

  Future<UserSession> googleSignIn() async {
    final userSession = await _userController.googleSignIn();
    await _setSession(userSession);
    return userSession;
  }

  Future<UserSession> googleSignInWithAccount(
    GoogleSignInAccount googleSignInAccount,
  ) async {
    final userSession = await _userController.googleSignInWithAccount(
      googleSignInAccount,
    );
    await _setSession(userSession);
    return userSession;
  }

  Future<UserSession> facebookSignIn() async {
    final userSession = await _userController.facebookSignIn();
    await _setSession(userSession);
    return userSession;
  }

  Future<void> logout() async {
    final userType = _session?.userType;
    try {
      if (userType != null) {
        await _userController.logout(userType);
      }
    } finally {
      await AppTokenStore.clear();
      _session = null;
      notifyListeners();
    }
  }

  Future<void> _setSession(UserSession userSession) async {
    await AppTokenStore.saveSession(userSession);
    _session = userSession;
    notifyListeners();
  }

  void _handleUnauthorized() {
    if (_session == null) {
      return;
    }

    _session = null;
    notifyListeners();
  }
}
