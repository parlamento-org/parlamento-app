import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/controllers/profile_controller.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/models/profile.dart';
import 'package:frontend/models/user.dart';
import 'package:frontend/pages/profile_page.dart';

void main() {
  testWidgets(
    'profile stats page swipes from general stats to topic breakdown',
    (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: ProfilePage(
            profileController: _FakeProfileController(_profileStats()),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Geral'), findsWidgets);
      expect(find.text('Visao geral').hitTestable(), findsOneWidget);
      expect(find.text('3 votos comparáveis').hitTestable(), findsNothing);

      await tester.drag(find.byType(ProfilePage), const Offset(-500, 0));
      await tester.pumpAndSettle();

      expect(find.text('Visao geral').hitTestable(), findsNothing);
      expect(find.text('3 votos comparáveis').hitTestable(), findsOneWidget);
      expect(find.text('PS'), findsWidgets);
    },
  );
}

class _FakeProfileController extends ProfileController {
  _FakeProfileController(ProfileStats profileStats)
    : super(repository: _FakeRepository(profileStats));
}

class _FakeRepository implements Repository {
  _FakeRepository(this._profileStats);

  final ProfileStats _profileStats;

  @override
  Future<ProfileStats> getProfileStats() async => _profileStats;

  @override
  Future<UserSession> currentSessionRequest() => throw UnimplementedError();

  @override
  Future<UserSession> facebookSignInRequest(
    String accessToken,
    String email,
    String name,
    int profilePicId,
  ) => throw UnimplementedError();

  @override
  Future<InitiativeFeedCard> getInitiativeFeedCard(
    ProposalFlowFeedRequest request,
  ) => throw UnimplementedError();

  @override
  Future<ProposalHistoryPage> getProposalHistory(
    ProposalHistoryRequest request,
  ) => throw UnimplementedError();

  @override
  Future<ProposalJourney> getProposalJourney(int initiativeId) =>
      throw UnimplementedError();

  @override
  Future<ProposalReveal> getProposalReveal({required int initiativeId}) =>
      throw UnimplementedError();

  @override
  Future<UserSession> googleSignInRequest(
    String idToken,
    String email,
    String name,
    int profilePicId,
  ) => throw UnimplementedError();

  @override
  Future<UserSession> loginRequest(String email, String password) =>
      throw UnimplementedError();

  @override
  Future<bool> registerRequest(
    String email,
    String userName,
    String password,
    int profilePicId,
  ) => throw UnimplementedError();

  @override
  Future<ProposalInteractionResult> recordProposalInteraction(
    ProposalInteractionSubmission submission,
  ) => throw UnimplementedError();
}

ProfileStats _profileStats() {
  return ProfileStats(
    overview: ProfileOverview(
      proposalsInteracted: 12,
      supportCount: 5,
      opposeCount: 4,
      abstentionCount: 2,
      skipCount: 1,
      supportRate: 41.7,
      skipRate: 8.3,
    ),
    partyAlignment: PartyAlignmentSection(
      isUnlocked: true,
      minimumComparableVotes: 10,
      minimumTopicComparableVotes: 2,
      totalComparableVotes: 11,
      parties: [
        PartyAlignment(
          partyId: 'PS',
          partyAcronym: 'PS',
          partyName: 'Partido Socialista',
          alignedCount: 9,
          comparableCount: 11,
          alignmentPercentage: 81.8,
        ),
      ],
      topicBreakdowns: [
        TopicPartyAlignment(
          parentTopicSlug: 'educacao_e_inclusao_social',
          parentTopicLabel: 'Educação e Inclusão',
          totalComparableVotes: 3,
          isLowData: false,
          parties: [
            PartyAlignment(
              partyId: 'PS',
              partyAcronym: 'PS',
              partyName: 'Partido Socialista',
              alignedCount: 3,
              comparableCount: 3,
              alignmentPercentage: 100,
            ),
          ],
        ),
      ],
    ),
  );
}
