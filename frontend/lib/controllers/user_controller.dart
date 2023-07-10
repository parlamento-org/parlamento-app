import 'package:frontend/fetcher/api_repository.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/user.dart';

class UserController {
  final Repository _repository = APIRepository();

  Future<UserSession> login(String email, String password) async {
    try {
      final user = await _repository.loginRequest(email, password);

      return user;
    } catch (e) {
      rethrow;
    }
  }
}
