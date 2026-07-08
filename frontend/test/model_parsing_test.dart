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
        'phases': [],
      });

      expect(journey.fullProposalTextLink, 'https://example.com/propostas/10');

      final withoutLink = ProposalJourney.fromJson({
        'initiativeId': 11,
        'initiativeType': 'Projeto de Lei',
        'title': 'Sem link',
      });

      expect(withoutLink.fullProposalTextLink, isNull);
      expect(withoutLink.phases, isEmpty);
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
        },
      });

      expect(profile.overview.proposalsInteracted, 12);
      expect(profile.overview.skipCount, 1);
      expect(profile.partyAlignment.isUnlocked, isTrue);
      expect(profile.partyAlignment.minimumComparableVotes, 10);
      expect(profile.partyAlignment.parties.single.partyAcronym, 'PS');
      expect(profile.partyAlignment.parties.single.alignedCount, 9);
      expect(profile.partyAlignment.parties.single.alignmentPercentage, 81.8);
    });
  });
}
