class EmailHasAccount implements Exception {
  final String message = 'Email already has an account!';

  EmailHasAccount();

  @override
  String toString() {
    return message;
  }
}
