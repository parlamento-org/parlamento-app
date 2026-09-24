import 'package:flutter/gestures.dart';
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
      await _pumpProfilePage(tester);

      expect(find.text('Geral'), findsWidgets);
      expect(find.text('Visao geral').hitTestable(), findsOneWidget);
      expect(find.text('3 votos comparáveis').hitTestable(), findsNothing);

      await _swipeToNextStatsPage(tester);

      expect(find.text('Visao geral').hitTestable(), findsNothing);
      expect(find.text('3 votos comparáveis').hitTestable(), findsOneWidget);
      expect(find.text('PS'), findsWidgets);
    },
  );

  testWidgets('selected topic tab stays visible after page swipes', (
    tester,
  ) async {
    await _pumpProfilePage(
      tester,
      surfaceSize: const Size(520, 800),
      parentTopics: _manyParentTopics,
    );

    for (var page = 0; page < 4; page++) {
      await _swipeToNextStatsPage(tester);
    }

    final selectedTopicTab = find.widgetWithText(
      ChoiceChip,
      _manyParentTopics[3].label,
    );
    expect(selectedTopicTab.hitTestable(), findsOneWidget);
  });

  testWidgets('desktop topic arrows move between stats pages', (tester) async {
    await _pumpProfilePage(
      tester,
      surfaceSize: const Size(1000, 800),
      parentTopics: _manyParentTopics,
    );

    expect(find.text('Visao geral').hitTestable(), findsOneWidget);

    await tester.tap(find.byTooltip('Próximo tópico'));
    await tester.pumpAndSettle();

    expect(find.text('Visao geral').hitTestable(), findsNothing);
    expect(
      find.text(_manyParentTopics.first.label).hitTestable(),
      findsWidgets,
    );
  });

  testWidgets('mouse wheel over topic tabs scrolls the tab strip', (
    tester,
  ) async {
    await _pumpProfilePage(
      tester,
      surfaceSize: const Size(520, 800),
      parentTopics: _manyParentTopics,
    );

    final lastTopicTab = find.widgetWithText(
      ChoiceChip,
      _manyParentTopics.last.label,
    );
    expect(lastTopicTab.hitTestable(), findsNothing);

    final tabCenter = tester.getCenter(
      find.widgetWithText(ChoiceChip, 'Geral'),
    );
    final mouse = await tester.createGesture(kind: PointerDeviceKind.mouse);
    await mouse.addPointer(location: tabCenter);
    addTearDown(mouse.removePointer);
    await tester.pump();

    await tester.sendEventToBinding(
      PointerScrollEvent(
        kind: PointerDeviceKind.mouse,
        position: tabCenter,
        scrollDelta: const Offset(0, 1400),
      ),
    );
    await tester.pumpAndSettle();

    expect(lastTopicTab.hitTestable(), findsOneWidget);
  });
}

Future<void> _pumpProfilePage(
  WidgetTester tester, {
  Size? surfaceSize,
  List<_ParentTopicFixture> parentTopics = _defaultParentTopics,
}) async {
  if (surfaceSize != null) {
    tester.view.physicalSize = surfaceSize;
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
  }

  await tester.pumpWidget(
    MaterialApp(
      home: ProfilePage(
        profileController: _FakeProfileController(
          _profileStats(parentTopics: parentTopics),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

Future<void> _swipeToNextStatsPage(WidgetTester tester) async {
  await tester.drag(find.byType(ProfilePage), const Offset(-500, 0));
  await tester.pumpAndSettle();
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

class _ParentTopicFixture {
  const _ParentTopicFixture({required this.slug, required this.label});

  final String slug;
  final String label;
}

const _defaultParentTopics = [
  _ParentTopicFixture(
    slug: 'educacao_e_inclusao_social',
    label: 'Educação e Inclusão',
  ),
];

const _manyParentTopics = [
  _ParentTopicFixture(
    slug: 'educacao_e_inclusao_social',
    label: 'Educação e Inclusão',
  ),
  _ParentTopicFixture(slug: 'saude_e_cuidados', label: 'Saúde e Cuidados'),
  _ParentTopicFixture(slug: 'habitacao', label: 'Habitação'),
  _ParentTopicFixture(slug: 'ambiente_e_clima', label: 'Ambiente e Clima'),
  _ParentTopicFixture(slug: 'economia', label: 'Economia'),
  _ParentTopicFixture(slug: 'justica', label: 'Justiça'),
  _ParentTopicFixture(slug: 'cultura', label: 'Cultura'),
  _ParentTopicFixture(slug: 'transportes', label: 'Transportes'),
];

ProfileStats _profileStats({
  List<_ParentTopicFixture> parentTopics = _defaultParentTopics,
}) {
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
      topicBreakdowns:
          parentTopics
              .map(
                (parentTopic) => TopicPartyAlignment(
                  parentTopicSlug: parentTopic.slug,
                  parentTopicLabel: parentTopic.label,
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
              )
              .toList(),
    ),
  );
}
