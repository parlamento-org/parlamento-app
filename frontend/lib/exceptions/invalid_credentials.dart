class InvalidCredentials implements Exception {
  final String message = 'Please type in a valid email and password!';

  InvalidCredentials();

  @override
  String toString() {
    return message;
  }
}
