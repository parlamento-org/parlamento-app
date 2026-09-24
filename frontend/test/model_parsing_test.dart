import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/models/proposal.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/models/profile.dart';
import 'package:frontend/models/vote_model.dart';

void main() {
  group('model parsing', () {
    test(
      'parses vote orientation wire values and serializes them unchanged',
      () {
        final vote = UserVote.fromJson({
          'userID': 1,
          'projectLawID': 10,
          'votingOrientation': 'InFavor',
          'voteDate': '2026-06-30',
        });

        expect(vote.voteOrientation, VoteOrientation.inFavor);
        expect(vote.toJson()['votingOrientation'], 'InFavor');
      },
    );

    test('parses proposal result and strips censored HTML text', () {
      final proposal = Proposal.fromJson({
        'proposalTitle': 'Clean Energy Bill',
        'id': 10,
        'legislatura': 'XV',
        'voteDate': '2026-06-30',
        'proposingParty': {
          'partyAcronym': 'PS',
          'fullName': 'Partido Socialista',
          'logoLink': 'https://example.com/ps.png',
        },
        'fullProposalTextLink': 'https://example.com/proposal',
        'proposalTextHTML': '<p>Texto <censored> &nbsp; final</p>',
        'proposalResult': 'ApprovedInGenerality',
        'votingResultGenerality': {
          'isUninamous': false,
          'votingBlocks': [
            {
              'isUninamousWithinParty': true,
              'numberOfDeputies': 10,
              'politicalPartyAcronym': 'PS',
              'votingOrientation': 'InFavor',
            },
          ],
        },
        'votingResultSpeciality': null,
      });

      expect(proposal.votingResult, VotingOutcome.approvedInGenerality);
      expect(proposal.censoredText, contains('CENSURADO'));
      expect(proposal.censoredText, isNot(contains('<p>')));
      expect(
        proposal.votingResultInGenerality.votingBlocks.single.voteOrientation,
        VoteOrientation.inFavor,
      );
    });

    test('parses paginated proposal history with missing optional fields', () {
      final page = ProposalHistoryPage.fromJson({
        'items': [
          {
            'interactionId': 12,
            'initiativeId': 99,
            'initiativeType': 'Projeto de Lei',
            'title': 'Histórico sem campos opcionais',
            'action': 'Support',
            'generalityVote': {
              'stageCode': '250',
              'stageName': 'Votação na generalidade',
              'result': 'Aprovado',
              'approved': true,
              'isUnanimous': false,
              'partyVotes': [],
            },
          },
        ],
        'page': 1,
        'pageSize': 20,
        'totalItems': 1,
        'totalPages': 1,
        'hasNextPage': false,
        'availableLegislatures': ['XVII', 'XVI'],
        'availableProposingParties': [
          {'acronym': 'PS', 'name': 'Partido Socialista'},
        ],
      });

      expect(page.items, hasLength(1));
      expect(page.items.single.legislature, isNull);
      expect(page.items.single.proposers, isEmpty);
      expect(page.items.single.action, ProposalInteractionAction.support);
      expect(page.items.single.generalityVote?.approved, isTrue);
      expect(page.hasPreviousPage, isFalse);
      expect(page.availableLegislatures, ['XVII', 'XVI']);
      expect(page.availableProposingParties.single.acronym, 'PS');
    });

    test('serializes proposal history filters as query parameters', () {
      final request = ProposalHistoryRequest(
        page: 2,
        pageSize: 10,
        filters: const ProposalHistoryFilters(
          legislature: 'XVII',
          proposingParty: 'PS',
          search: 'energia',
          interactionType: ProposalInteractionAction.support,
        ),
      );

      expect(request.toQueryParameters(), {
        'page': '2',
        'pageSize': '10',
        'legislature': 'XVII',
        'proposingParty': 'PS',
        'search': 'energia',
        'interactionType': 'Support',
      });
    });

    test('parses proposal journey original document link', () {
      final journey = ProposalJourney.fromJson({
        'initiativeId': 10,
        'initiativeType': 'Projeto de Lei',
        'initiativeNumber': '10/XV/1',
        'title': 'Titulo da iniciativa',
        'fullProposalTextLink': 'https://example.com/propostas/10',
        'userVote': 'Support',
        'generalityVote': {
          'stageCode': '250',
          'stageName': 'Votação na generalidade',
          'result': 'Aprovado',
          'approved': true,
          'isUnanimous': false,
          'partyVotes': [],
        },
        'proposers': [
          {
            'kind': 'ParliamentaryGroup',
            'name': 'Partido Socialista',
            'acronym': 'PS',
          },
        ],
        'topicAssignments': [
          {
            'parentTopicSlug': 'ambiente',
            'parentTopicLabel': 'Ambiente',
            'subtopicSlug': 'energia',
            'subtopicLabel': 'Energia',
            'assignmentStatus': 'assigned',
          },
        ],
        'phases': [],
      });

      expect(journey.fullProposalTextLink, 'https://example.com/propostas/10');
      expect(journey.userVote, ProposalInteractionAction.support);
      expect(journey.generalityVote?.approved, isTrue);
      expect(journey.proposers.single.acronym, 'PS');
      expect(journey.topicAssignments.single.parentTopicLabel, 'Ambiente');
      expect(journey.topicAssignments.single.subtopicLabel, 'Energia');

      final withoutLink = ProposalJourney.fromJson({
        'initiativeId': 11,
        'initiativeType': 'Projeto de Lei',
        'title': 'Sem link',
      });

      expect(withoutLink.fullProposalTextLink, isNull);
      expect(withoutLink.proposers, isEmpty);
      expect(withoutLink.topicAssignments, isEmpty);
      expect(withoutLink.phases, isEmpty);
    });

    test('parses proposal topic assignments on feed and reveal responses', () {
      final feedCard = InitiativeFeedCard.fromJson({
        'initiativeId': 10,
        'initiativeType': 'Projeto de Lei',
        'neutralTitle': 'Titulo neutro',
        'topicAssignments': [
          {
            'parentTopicSlug': 'ambiente',
            'parentTopicLabel': 'Ambiente',
            'subtopicSlug': 'energia',
            'subtopicLabel': 'Energia',
            'assignmentStatus': 'assigned',
            'assignmentConfidence': 92.4,
          },
        ],
      });

      expect(feedCard.topicAssignments, hasLength(1));
      expect(feedCard.topicAssignments.single.parentTopicLabel, 'Ambiente');
      expect(feedCard.topicAssignments.single.subtopicLabel, 'Energia');
      expect(feedCard.topicAssignments.single.assignmentConfidence, 92.4);

      final reveal = ProposalReveal.fromJson({
        'initiativeId': 10,
        'initiativeType': 'Projeto de Lei',
        'title': 'Titulo revelado',
        'userVote': 'Support',
        'proposers': [],
        'officialSources': [],
        'journey': {'label': 'Percurso', 'endpoint': '/journey'},
        'topicAssignments': [
          {
            'parentTopicSlug': 'educacao',
            'parentTopicLabel': 'Educação',
            'subtopicSlug': 'escolas',
            'subtopicLabel': 'Escolas',
            'assignmentStatus': 'accepted_cluster',
          },
        ],
      });

      expect(reveal.topicAssignments.single.parentTopicSlug, 'educacao');
      expect(
        reveal.topicAssignments.single.assignmentStatus,
        'accepted_cluster',
      );
    });

    test('parses profile overview and party alignment metadata', () {
      final profile = ProfileStats.fromJson({
        'overview': {
          'proposalsInteracted': 12,
          'supportCount': 5,
          'opposeCount': 4,
          'abstentionCount': 2,
          'skipCount': 1,
          'supportRate': 41.7,
          'skipRate': 8.3,
        },
        'partyAlignment': {
          'isUnlocked': true,
          'minimumComparableVotes': 10,
          'minimumTopicComparableVotes': 2,
          'totalComparableVotes': 11,
          'parties': [
            {
              'partyId': 'PS',
              'partyAcronym': 'PS',
              'partyName': 'Partido Socialista',
              'partyLogo': 'https://example.com/ps.png',
              'alignedCount': 9,
              'comparableCount': 11,
              'alignmentPercentage': 81.8,
            },
          ],
          'topicBreakdowns': [
            {
              'parentTopicSlug': 'educacao',
              'parentTopicLabel': 'Educação',
              'totalComparableVotes': 3,
              'isLowData': false,
              'parties': [
                {
                  'partyId': 'PS',
                  'partyAcronym': 'PS',
                  'partyName': 'Partido Socialista',
                  'alignedCount': 3,
                  'comparableCount': 3,
                  'alignmentPercentage': 100,
                },
              ],
            },
            {
              'parentTopicSlug': 'mobilidade',
              'parentTopicLabel': 'Mobilidade',
              'totalComparableVotes': 1,
              'isLowData': true,
              'parties': [],
            },
          ],
        },
      });

      expect(profile.overview.proposalsInteracted, 12);
      expect(profile.overview.skipCount, 1);
      expect(profile.partyAlignment.isUnlocked, isTrue);
      expect(profile.partyAlignment.minimumComparableVotes, 10);
      expect(profile.partyAlignment.parties.single.partyAcronym, 'PS');
      expect(profile.partyAlignment.parties.single.alignedCount, 9);
      expect(profile.partyAlignment.parties.single.alignmentPercentage, 81.8);
      expect(profile.partyAlignment.minimumTopicComparableVotes, 2);
      expect(profile.partyAlignment.topicBreakdowns, hasLength(2));
      expect(
        profile.partyAlignment.topicBreakdowns.first.parentTopicLabel,
        'Educação',
      );
      expect(
        profile
            .partyAlignment
            .topicBreakdowns
            .first
            .parties
            .single
            .alignedCount,
        3,
      );
      expect(profile.partyAlignment.topicBreakdowns.last.isLowData, isTrue);
    });
  });
}
