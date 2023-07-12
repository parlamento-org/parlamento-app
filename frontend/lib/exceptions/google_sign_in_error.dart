class GoogleSignInError implements Exception {
  final String message = 'Google Sign In Error';

  GoogleSignInError();

  @override
  String toString() => message;
}
