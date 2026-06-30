import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/models/proposal.dart';
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
  });
}
