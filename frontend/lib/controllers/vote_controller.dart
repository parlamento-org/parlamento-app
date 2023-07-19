import 'package:frontend/fetcher/api_repository.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/proposal.dart';
import 'package:frontend/models/proposal_criteria.dart';

import '../models/vote_model.dart';

class VoteController {
  final Repository _repository = APIRepository();

  Future<Proposal> getRandomProposal(ProposalCriteria criteria) async {
    return await _repository.getProposal(criteria);
  }

  Future<void> castUserVote(UserVote userVote) async {
    await _repository.castUserVote(userVote);
  }
}
