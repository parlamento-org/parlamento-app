import 'package:flutter/material.dart';
import 'package:frontend/models/proposal.dart';
import 'package:frontend/models/vote_model.dart';
import 'package:frontend/pages/proposal_result.dart';
import 'package:frontend/pages/vote_page.dart';

class VotingSection extends StatefulWidget {
  const VotingSection({super.key});

  @override
  State<VotingSection> createState() => _MyVotingSectionState();
}

class _MyVotingSectionState extends State<VotingSection> {
  void displayProposalResults(
      Proposal proposal, VoteOrientation userVoteOrientation) {
    setState(() {
      _proposalResult = ProposalResult(
          userVoteOrientation: userVoteOrientation,
          proposal: proposal,
          nextStep: proposal.votingResultInSpeciality == null
              ? displayVotePage
              : displaySpecialityVotePage,
          votingStage: VotingStage.generality);
      _displayVotePage = false;
    });
  }

  late VotePage _votePage = VotePage(
    displayProposalResults: displayProposalResults,
  );
  bool _displayVotePage = true;
  ProposalResult? _proposalResult;

  void displaySpecialityVotePage(
      Proposal proposal, VoteOrientation userVoteOrientation) {
    setState(() {
      _proposalResult = ProposalResult(
          userVoteOrientation: userVoteOrientation,
          proposal: proposal,
          nextStep: displayVotePage,
          votingStage: VotingStage.generality);
      _displayVotePage = false;
    });
  }

  void displayVotePage() {
    setState(() {
      _votePage = VotePage(
        displayProposalResults: displayProposalResults,
      );
      _displayVotePage = true;
    });
  }

  @override
  Widget build(BuildContext context) {
    return _displayVotePage ? _votePage : _proposalResult!;
  }
}
