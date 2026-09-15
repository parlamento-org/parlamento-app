import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/models/profile.dart';

import '../models/user.dart';

abstract class Repository {
  Future<UserSession> loginRequest(String email, String password);

  Future<UserSession> currentSessionRequest();

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

  Future<InitiativeFeedCard> getInitiativeFeedCard(
    ProposalFlowFeedRequest request,
  );

  Future<ProposalInteractionResult> recordProposalInteraction(
    ProposalInteractionSubmission submission,
  );

  Future<ProposalReveal> getProposalReveal({required int initiativeId});

  Future<ProposalJourney> getProposalJourney(int initiativeId);

  Future<ProposalHistoryPage> getProposalHistory(
    ProposalHistoryRequest request,
  );

  Future<ProfileStats> getProfileStats();
}
