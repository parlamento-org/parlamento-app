import 'package:flutter/foundation.dart';
import 'package:frontend/controllers/user_controller.dart';
import 'package:frontend/models/user.dart';

class AuthController extends ChangeNotifier {
  AuthController({UserController? userController})
    : _userController = userController ?? UserController();

  final UserController _userController;
  UserSession? _session;

  UserSession? get session => _session;
  bool get isLoggedIn => _session?.isLoggedIn ?? false;

  Future<UserSession> login(String email, String password) async {
    final userSession = await _userController.login(email, password);
    _setSession(userSession);
    return userSession;
  }

  Future<UserSession> googleSignIn() async {
    final userSession = await _userController.googleSignIn();
    _setSession(userSession);
    return userSession;
  }

  Future<UserSession> facebookSignIn() async {
    final userSession = await _userController.facebookSignIn();
    _setSession(userSession);
    return userSession;
  }

  Future<void> logout() async {
    final userType = _session?.userType;
    if (userType != null) {
      await _userController.logout(userType);
    }
    _session = null;
    notifyListeners();
  }

  void _setSession(UserSession userSession) {
    _session = userSession;
    notifyListeners();
  }
}
