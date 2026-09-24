import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/controllers/vote_controller.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/models/profile.dart';
import 'package:frontend/models/user.dart';
import 'package:frontend/pages/proposal_journey_page.dart';
import 'package:frontend/widgets/parliamentary_vote_breakdown.dart';

void main() {
  testWidgets(
    'shows fallback topics, proposer logo, and human readable dates',
    (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: ProposalJourneyPage(
            initiativeId: 10,
            initialProposers: [
              ProposalProposer(
                kind: 'ParliamentaryGroup',
                name: 'Partido Socialista',
                acronym: 'PS',
              ),
            ],
            initialTopicAssignments: [
              ProposalTopicAssignment(
                parentTopicSlug: 'ambiente',
                parentTopicLabel: 'Ambiente',
                subtopicSlug: 'energia',
                subtopicLabel: 'Energia',
                assignmentStatus: 'assigned',
              ),
            ],
            voteController: VoteController(
              repository: _FakeRepository(
                ProposalJourney(
                  initiativeId: 10,
                  initiativeType: 'Projeto de Lei',
                  initiativeNumber: '40',
                  legislature: 'XV',
                  initiativeSelection: '1',
                  title: 'Titulo da iniciativa',
                  userVote: ProposalInteractionAction.support,
                  generalityVote: ParliamentaryVoteSummary(
                    stageCode: '250',
                    stageName: 'Votação na generalidade',
                    result: 'Aprovado',
                    approved: true,
                    isUnanimous: false,
                    partyVotes: const [],
                  ),
                  phases: [
                    ProposalJourneyPhase(
                      phaseCode: '250',
                      phaseName: 'Votação na generalidade',
                      date: '2026-06-30',
                      summary: 'Resumo da fase',
                      approvedTextId: 'AT-123',
                      votes: [
                        ParliamentaryVoteSummary(
                          stageCode: '250',
                          stageName: 'Votação na generalidade',
                          date: '28-09-2019',
                          isUnanimous: false,
                          partyVotes: const [],
                        ),
                      ],
                      documents: const [],
                      diaryLinks: const [],
                      videos: [
                        ProposalJourneyVideo(
                          speakerName: 'Deputada Exemplo',
                          date: '2024-01-05',
                          url: 'https://example.com/video',
                        ),
                      ],
                      transcripts: const [],
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('Projeto de Lei nº 40 / XV / 1'), findsOneWidget);
      expect(find.text('Legislatura XV'), findsOneWidget);
      expect(find.text('40/XV/1'), findsNothing);
      expect(find.text('O teu voto'), findsOneWidget);
      expect(find.text('Tu'), findsOneWidget);
      expect(find.text('Parlamento'), findsOneWidget);
      expect(find.byType(ParliamentaryPartyLogo), findsOneWidget);
      expect(find.text('Ambiente'), findsOneWidget);
      expect(find.text('Energia'), findsOneWidget);
      expect(find.text('30 de junho de 2026'), findsOneWidget);
      expect(find.text('28 de setembro de 2019'), findsOneWidget);
      expect(find.textContaining('5 de janeiro de 2024'), findsOneWidget);
      expect(find.text('2026-06-30'), findsNothing);
      expect(find.text('28-09-2019'), findsNothing);
      expect(find.text('Fase 250'), findsNothing);
      expect(find.text('AT-123'), findsNothing);
    },
  );

  testWidgets('only shows the user vote chip in phase 250', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: ProposalJourneyPage(
          initiativeId: 10,
          voteController: VoteController(
            repository: _FakeRepository(
              ProposalJourney(
                initiativeId: 10,
                initiativeType: 'Projeto de Lei',
                title: 'Titulo da iniciativa',
                userVote: ProposalInteractionAction.support,
                phases: [
                  ProposalJourneyPhase(
                    phaseCode: '200',
                    phaseName: 'Discussão na generalidade',
                    summary: 'Resumo da discussão.',
                    votes: const [],
                    documents: const [],
                    diaryLinks: const [],
                    videos: const [],
                    transcripts: const [],
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Discussão na generalidade'), findsOneWidget);
    expect(find.text('Tu'), findsNothing);
  });
}

class _FakeRepository implements Repository {
  _FakeRepository(this._journey);

  final ProposalJourney _journey;

  @override
  Future<ProposalJourney> getProposalJourney(int initiativeId) async =>
      _journey;

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
  Future<ProfileStats> getProfileStats() => throw UnimplementedError();

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
