import 'package:frontend/models/proposal.dart';
import 'package:frontend/models/proposal_flow.dart';

import '../models/proposal_criteria.dart';
import '../models/user.dart';
import '../models/vote_model.dart';

abstract class Repository {
  Future<UserSession> loginRequest(String email, String password);

  Future<bool> registerRequest(
    String email,
    String userName,
    String password,
    int profilePicId,
  );

  Future<UserSession> googleSignInRequest(
    String idToken,
    String email,
    String name,
    int profilePicId,
  );

  Future<UserSession> facebookSignInRequest(
    String accessToken,
    String email,
    String name,
    int profilePicId,
  );

  Future<Proposal> getProposal(ProposalCriteria proposalCriteria);

  Future<void> castUserVote(UserVote userVote);

  Future<InitiativeFeedCard> getInitiativeFeedCard(
    ProposalFlowFeedRequest request,
  );

  Future<ProposalInteractionResult> recordProposalInteraction(
    ProposalInteractionSubmission submission,
  );

  Future<ProposalReveal> getProposalReveal({required int initiativeId});

  Future<ProposalJourney> getProposalJourney(int initiativeId);

  Future<List<ProposalHistoryItem>> getProposalHistory();
}
