import 'package:frontend/fetcher/api_repository.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/proposal_flow.dart';

class VoteController {
  VoteController({Repository? repository})
    : _repository = repository ?? APIRepository();

  final Repository _repository;

  Future<InitiativeFeedCard> getInitiativeFeedCard(
    ProposalFlowFeedRequest request,
  ) {
    return _repository.getInitiativeFeedCard(request);
  }

  Future<ProposalInteractionResult> recordProposalInteraction(
    ProposalInteractionSubmission submission,
  ) {
    return _repository.recordProposalInteraction(submission);
  }
}
