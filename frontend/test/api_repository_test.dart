import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/exceptions/email_has_account.dart';
import 'package:frontend/exceptions/invalid_credentials.dart';
import 'package:frontend/exceptions/username_already_exists.dart';
import 'package:frontend/fetcher/api_client.dart';
import 'package:frontend/fetcher/api_repository.dart';
import 'package:frontend/models/proposal_criteria.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  group('APIRepository', () {
    test('maps login failures to InvalidCredentials', () async {
      final repository = _repositoryWithResponse(statusCode: 401);

      expect(
        () => repository.loginRequest('person@example.com', 'wrong-password'),
        throwsA(isA<InvalidCredentials>()),
      );
    });

    test('maps duplicate email registration response', () async {
      final repository = _repositoryWithResponse(statusCode: 401);

      expect(
        () => repository.registerRequest(
          'person@example.com',
          'person',
          'password',
          0,
        ),
        throwsA(isA<EmailHasAccount>()),
      );
    });

    test('maps duplicate username registration response', () async {
      final repository = _repositoryWithResponse(statusCode: 402);

      expect(
        () => repository.registerRequest(
          'person@example.com',
          'person',
          'password',
          0,
        ),
        throwsA(isA<UsernameAlreadyExists>()),
      );
    });

    test('throws typed API exception for proposal feed failures', () async {
      final repository = _repositoryWithResponse(statusCode: 500);

      expect(
        () => repository.getProposal(
          ProposalCriteria(userID: 1, lowestScoreAllowed: 0),
        ),
        throwsA(isA<ApiException>()),
      );
    });
  });
}

APIRepository _repositoryWithResponse({required int statusCode}) {
  return APIRepository(
    apiClient: ApiClient(
      baseUrl: Uri.parse('http://localhost:8180'),
      httpClient: MockClient((request) async {
        return http.Response('{}', statusCode);
      }),
    ),
  );
}
