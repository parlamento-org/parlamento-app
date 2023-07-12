class UsernameAlreadyExists implements Exception {
  final String message = 'Username already exists!';

  UsernameAlreadyExists();

  @override
  String toString() {
    return message;
  }
}
