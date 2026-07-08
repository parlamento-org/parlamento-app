import 'package:flutter/foundation.dart';
import 'package:frontend/auth/token_storage.dart';
import 'package:frontend/controllers/user_controller.dart';
import 'package:frontend/models/user.dart';
import 'package:google_sign_in/google_sign_in.dart';

class AuthController extends ChangeNotifier {
  AuthController({
    UserController? userController,
    bool restoreSessionOnStart = true,
  })
    : _userController = userController ?? UserController() {
    AppTokenStore.onUnauthorized = _handleUnauthorized;
    if (restoreSessionOnStart) {
      _restoreSession();
    } else {
      _isInitializing = false;
    }
  }

  final UserController _userController;
  UserSession? _session;
  bool _isInitializing = true;

  UserSession? get session => _session;
  bool get isInitializing => _isInitializing;
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
    _isInitializing = false;
    notifyListeners();
  }

  Future<void> _restoreSession() async {
    final storedToken = await AppTokenStore.currentAccessToken();
    if (storedToken == null) {
      _finishInitialization();
      return;
    }

    try {
      final userSession = await _userController.restoreSession();
      await _setSession(userSession);
    } catch (_) {
      await AppTokenStore.clear();
      _session = null;
      _finishInitialization();
    }
  }

  void _finishInitialization() {
    if (!_isInitializing) {
      return;
    }

    _isInitializing = false;
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
