class InvalidEmail implements Exception {
  final String message = 'The email you entered is invalid!';

  InvalidEmail();

  @override
  String toString() {
    return message;
  }
}
