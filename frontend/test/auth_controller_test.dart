import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/auth/token_storage.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/user_controller.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/proposal.dart';
import 'package:frontend/models/proposal_criteria.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/models/profile.dart';
import 'package:frontend/models/user.dart';
import 'package:frontend/models/vote_model.dart';

void main() {
  setUp(() {
    AppTokenStore.storage = InMemoryTokenStorage();
    AppTokenStore.onUnauthorized = null;
  });

  group('AuthController', () {
    test('stores the session and notifies listeners after login', () async {
      final repository = _FakeRepository(
        userSession: _userSession(userId: 42, userType: UserType.email),
      );
      final controller = AuthController(
        userController: UserController(repository: repository),
        restoreSessionOnStart: false,
      );
      var notifications = 0;
      controller.addListener(() => notifications++);

      final session = await controller.login('person@example.com', 'password');

      expect(session.userId, 42);
      expect(controller.session, same(session));
      expect(controller.isLoggedIn, isTrue);
      expect(notifications, 1);
      expect(await AppTokenStore.currentAccessToken(), 'test-token');
    });

    test('clears the session and notifies listeners on logout', () async {
      final repository = _FakeRepository(
        userSession: _userSession(userId: 7, userType: UserType.email),
      );
      final controller = AuthController(
        userController: UserController(repository: repository),
        restoreSessionOnStart: false,
      );
      await controller.login('person@example.com', 'password');

      await controller.logout();

      expect(controller.session, isNull);
      expect(controller.isLoggedIn, isFalse);
    });

    test('restores a stored session on startup', () async {
      final session = _userSession(userId: 12, userType: UserType.email);
      final repository = _FakeRepository(userSession: session);
      await AppTokenStore.saveSession(session);

      final controller = AuthController(
        userController: UserController(repository: repository),
      );
      final initialized = Future.doWhile(() async {
        if (!controller.isInitializing) {
          return false;
        }

        await Future<void>.delayed(Duration.zero);
        return true;
      });

      await initialized;

      expect(controller.session?.userId, 12);
      expect(controller.isLoggedIn, isTrue);
    });
  });
}

UserSession _userSession({required int userId, required UserType userType}) {
  return UserSession(
    name: 'Test User',
    userId: userId,
    email: 'person@example.com',
    profilePictureId: 0,
    partyStats: const [],
    userVotes: const [],
    userType: userType,
    accessToken: 'test-token',
    expiresAtUtc: DateTime.now().toUtc().add(const Duration(hours: 1)),
  );
}

class _FakeRepository implements Repository {
  _FakeRepository({required this.userSession});

  final UserSession userSession;

  @override
  Future<UserSession> currentSessionRequest() async {
    return userSession;
  }

  @override
  Future<void> castUserVote(UserVote userVote) async {}

  @override
  Future<UserSession> facebookSignInRequest(
    String accessToken,
    String email,
    String name,
    int profilePicId,
  ) async {
    return userSession;
  }

  @override
  Future<Proposal> getProposal(ProposalCriteria proposalCriteria) {
    throw UnimplementedError();
  }

  @override
  Future<InitiativeFeedCard> getInitiativeFeedCard(
    ProposalFlowFeedRequest request,
  ) {
    throw UnimplementedError();
  }

  @override
  Future<ProposalJourney> getProposalJourney(int initiativeId) {
    throw UnimplementedError();
  }

  @override
  Future<ProposalHistoryPage> getProposalHistory(
    ProposalHistoryRequest request,
  ) {
    throw UnimplementedError();
  }

  @override
  Future<ProfileStats> getProfileStats() {
    throw UnimplementedError();
  }

  @override
  Future<ProposalReveal> getProposalReveal({required int initiativeId}) {
    throw UnimplementedError();
  }

  @override
  Future<ProposalInteractionResult> recordProposalInteraction(
    ProposalInteractionSubmission submission,
  ) {
    throw UnimplementedError();
  }

  @override
  Future<UserSession> googleSignInRequest(
    String idToken,
    String email,
    String name,
    int profilePicId,
  ) async {
    return userSession;
  }

  @override
  Future<UserSession> loginRequest(String email, String password) async {
    return userSession;
  }

  @override
  Future<bool> registerRequest(
    String email,
    String userName,
    String password,
    int profilePicId,
  ) async {
    return true;
  }
}
