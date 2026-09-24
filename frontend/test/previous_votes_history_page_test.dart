import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/controllers/vote_controller.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/models/profile.dart';
import 'package:frontend/models/user.dart';
import 'package:frontend/pages/previous_votes_history_page.dart';

void main() {
  testWidgets('shows explicit vote labels and initiative metadata pill', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: PreviousVotesHistoryPage(
            voteController: VoteController(
              repository: _FakeRepository(
                item: ProposalHistoryItem(
                  interactionId: 12,
                  initiativeId: 99,
                  initiativeType: 'Projeto de Lei',
                  initiativeNumber: '40',
                  title: 'Titulo da iniciativa',
                  legislature: 'XV',
                  initiativeSelection: '1',
                  action: ProposalInteractionAction.support,
                  generalityVote: ParliamentaryVoteSummary(
                    stageCode: '250',
                    stageName: 'Votação na generalidade',
                    result: 'Aprovado',
                    approved: true,
                    isUnanimous: false,
                    partyVotes: const [],
                  ),
                  proposers: const [],
                ),
              ),
            ),
          ),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('O teu voto'), findsOneWidget);
    expect(find.text('Parlamento (Generalidade)'), findsOneWidget);
    expect(find.text('AR'), findsNothing);
    expect(find.text('Projeto de Lei nº 40 / XV / 1'), findsOneWidget);
    expect(find.text('Legislatura XV'), findsNothing);
  });

  testWidgets(
    'hides initiative metadata pill when its source number is missing',
    (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: PreviousVotesHistoryPage(
              voteController: VoteController(
                repository: _FakeRepository(
                  item: ProposalHistoryItem(
                    interactionId: 12,
                    initiativeId: 99,
                    initiativeType: 'Projeto de Lei',
                    title: 'Titulo da iniciativa',
                    action: ProposalInteractionAction.support,
                    proposers: const [],
                  ),
                ),
              ),
            ),
          ),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.textContaining('Projeto de Lei nº'), findsNothing);
    },
  );

  testWidgets('filters history by parent topic', (tester) async {
    final repository = _FakeRepository(
      item: ProposalHistoryItem(
        interactionId: 12,
        initiativeId: 99,
        initiativeType: 'Projeto de Lei',
        title: 'Titulo da iniciativa',
        action: ProposalInteractionAction.support,
        proposers: const [],
      ),
      availableParentTopics: [
        ProposalHistoryParentTopic(slug: 'educacao', label: 'Educação'),
      ],
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: PreviousVotesHistoryPage(
            voteController: VoteController(repository: repository),
          ),
        ),
      ),
    );

    await tester.pumpAndSettle();
    await tester.tap(find.byTooltip('Mostrar filtros'));
    await tester.pumpAndSettle();

    expect(find.text('Tópico'), findsOneWidget);
    await tester.tap(find.widgetWithText(ChoiceChip, 'Educação'));
    await tester.pumpAndSettle();

    expect(repository.requests.last.filters.parentTopicSlug, 'educacao');
    expect(find.text('Tópico: Educação'), findsOneWidget);
  });
}

class _FakeRepository implements Repository {
  _FakeRepository({required this.item, this.availableParentTopics = const []});

  final ProposalHistoryItem item;
  final List<ProposalHistoryParentTopic> availableParentTopics;
  final List<ProposalHistoryRequest> requests = [];

  @override
  Future<UserSession> loginRequest(String email, String password) =>
      throw UnimplementedError();

  @override
  Future<ProposalHistoryPage> getProposalHistory(
    ProposalHistoryRequest request,
  ) async {
    requests.add(request);

    return ProposalHistoryPage(
      items: [item],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
      availableLegislatures: const ['XV'],
      availableProposingParties: const [],
      availableParentTopics: availableParentTopics,
    );
  }

  @override
  Future<UserSession> currentSessionRequest() => throw UnimplementedError();

  @override
  Future<bool> registerRequest(
    String email,
    String userName,
    String password,
    int profilePicId,
  ) => throw UnimplementedError();

  @override
  Future<UserSession> googleSignInRequest(
    String idToken,
    String email,
    String name,
    int profilePicId,
  ) => throw UnimplementedError();

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
  Future<ProposalInteractionResult> recordProposalInteraction(
    ProposalInteractionSubmission submission,
  ) => throw UnimplementedError();

  @override
  Future<ProposalJourney> getProposalJourney(int initiativeId) =>
      throw UnimplementedError();

  @override
  Future<ProfileStats> getProfileStats() => throw UnimplementedError();

  @override
  Future<ProposalReveal> getProposalReveal({required int initiativeId}) =>
      throw UnimplementedError();
}
