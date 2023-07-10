import '../models/user.dart';

abstract class Repository {
  Future<UserSession> loginRequest(String email, String password);
}
