import '../models/user.dart';

abstract class Repository {
  Future<UserSession> loginRequest(String email, String password);

  Future<bool> registerRequest(
      String email, String userName, String password, int profilePicId);
}
