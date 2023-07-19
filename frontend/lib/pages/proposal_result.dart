import 'package:flutter/material.dart';
import 'package:frontend/models/vote_model.dart';
import 'package:frontend/models/voting_block.dart';

import '../models/proposal.dart';
import '../themes/base_theme.dart';

enum VotingStage { generality, speciality }

// ignore: must_be_immutable
class ProposalResult extends StatelessWidget {
  ProposalResult(
      {super.key,
      required this.proposal,
      required this.nextStep,
      required this.userVoteOrientation,
      required this.votingStage}) {
    if (votingStage == VotingStage.generality) {
      proposalApproved =
          proposal.votingResult == VotingOutcome.ApprovedInGenerality;
    } else {
      proposalApproved =
          proposal.votingResult == VotingOutcome.ApprovedInSpeciality;
    }

    partiesVotingFor = [];
    partiesVotingAgainst = [];
    partiesAbstaining = [];
    var votingBlocks = votingStage == VotingStage.generality
        ? proposal.votingResultInGenerality.votingBlocks
        : proposal.votingResultInSpeciality!.votingBlocks;
    if (proposal.votingResultInGenerality.isUnanimous) isUnanimous = true;
    for (var votingBlock in votingBlocks) {
      if (votingBlock.voteOrientation == VoteOrientation.InFavor) {
        partiesVotingFor.add(votingBlock);
      } else if (votingBlock.voteOrientation == VoteOrientation.Against) {
        partiesVotingAgainst.add(votingBlock);
      } else {
        partiesAbstaining.add(votingBlock);
      }
    }
  }

  late bool isUnanimous;
  final VoteOrientation userVoteOrientation;
  late List<VotingBlock> partiesVotingFor;
  late List<VotingBlock> partiesVotingAgainst;
  late List<VotingBlock> partiesAbstaining;
  final VotingStage votingStage;
  final Proposal proposal;
  late bool proposalApproved;
  final Function nextStep;

  Row generateRowVotedAbstained(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.start,
      children: [
        Image.asset("lib/images/voto_abster.png",
            height: MediaQuery.of(context).size.height * 0.1),
        const SizedBox(width: 10),
        Expanded(
            child: Wrap(
          spacing: 8.0,
          children: partiesAbstaining
              .map((e) => Image.asset('lib/images/${e.politicalParty}_logo.png',
                  height: 40))
              .toList(),
        ))
      ],
    );
  }

  Row generateRowVotedInFavor(BuildContext context) {
    return Row(mainAxisAlignment: MainAxisAlignment.start, children: [
      Image.asset("lib/images/voto_favor.png",
          height: MediaQuery.of(context).size.height * 0.1),
      const SizedBox(width: 10),
      Expanded(
          child: Wrap(
        spacing: 8.0,
        children: partiesVotingFor
            .map((e) => Image.asset('lib/images/${e.politicalParty}_logo.png',
                height: 40))
            .toList(),
      ))
    ]);
  }

  Row generateRowVotedAgainst(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.start,
      children: [
        Image.asset(
          "lib/images/voto_contra.png",
          height: MediaQuery.of(context).size.height * 0.1,
        ),
        const SizedBox(width: 10),
        Expanded(
            child: Wrap(
          spacing: 8.0,
          children: partiesVotingAgainst
              .map((e) => Image.asset('lib/images/${e.politicalParty}_logo.png',
                  height: 40))
              .toList(),
        ))
      ],
    );
  }

  Widget generateSelectedContainer(BuildContext context, Row row) {
    return Column(
      children: [
        Transform.translate(
            offset: const Offset(0, 15),
            child: Container(
              width: MediaQuery.of(context).size.width * 0.3,
              alignment: Alignment.center,
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(18.0),
                border: Border.all(
                  color: Colors.white,
                  width: 2.0,
                ),
              ),
              child: const Text(
                "Votaste com: ",
                style: TextStyle(
                  color: Colors.black,
                  fontSize: 16,
                ),
              ),
            )),
        Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: Colors.white.withOpacity(0.65),
              borderRadius: BorderRadius.circular(18.0),
              border: Border.all(
                color: Colors.white,
                width: 2.0,
              ),
            ),
            child: row)
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return Container(
        color: baseTheme.colorScheme.background,
        padding: const EdgeInsets.only(
            top: 16.0, bottom: 16.0, left: 32.0, right: 32.0),
        child: SingleChildScrollView(
          child: Column(children: [
            Container(
                decoration: BoxDecoration(
                  color: proposalApproved
                      ? approvedGreenNormal
                      : rejectedRedNormal,
                  borderRadius: BorderRadius.circular(18.0),
                  border: Border.all(
                    color: proposalApproved
                        ? approvedGreenNormal
                        : rejectedRedNormal,
                    width: 2.0,
                  ),
                ),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Transform.translate(
                        offset: const Offset(0, -15),
                        child: Container(
                          alignment: Alignment.center,
                          width: MediaQuery.of(context).size.width * 0.5,
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color: proposalApproved
                                ? approvedGreenBold
                                : rejectedRedBold,
                            borderRadius: BorderRadius.circular(18.0),
                            border: Border.all(
                              color: proposalApproved
                                  ? approvedGreenBold
                                  : rejectedRedBold,
                              width: 2.0,
                            ),
                          ),
                          child: Text(
                            "Resultados - ${votingStage == VotingStage.generality ? "Generalidade" : "Especialidade"}",
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 16,
                            ),
                          ),
                        )),
                    Row(mainAxisAlignment: MainAxisAlignment.center, children: [
                      proposalApproved
                          ? Image.asset(
                              "lib/images/voto_favor.png",
                              height: 50,
                            )
                          : Image.asset(
                              "lib/images/voto_contra.png",
                              height: 50,
                            ),
                      const SizedBox(width: 10),
                      Text(
                        proposalApproved ? "Aprovado" : "Rejeitado",
                        style: const TextStyle(
                            fontSize: 32,
                            fontWeight: FontWeight.w900,
                            color: Colors.white),
                      ),
                    ]),
                    const SizedBox(height: 10),
                    Container(
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(18.0),
                        border: Border.all(
                          color: Colors.white,
                          width: 2.0,
                        ),
                      ),
                      child: const Text(
                        "Proposto por:",
                        style: TextStyle(
                          color: Colors.black,
                          fontSize: 20,
                        ),
                      ),
                    ),
                    Image.asset(
                        "lib/images/${proposal.proposingParty.partyAcronym}_logo.png",
                        height: 70),
                    const SizedBox(height: 10),
                    Container(
                        width: MediaQuery.of(context).size.width * 0.8,
                        child: Column(
                            crossAxisAlignment: CrossAxisAlignment.center,
                            children: [
                              userVoteOrientation == VoteOrientation.InFavor
                                  ? generateSelectedContainer(
                                      context, generateRowVotedInFavor(context))
                                  : generateRowVotedInFavor(context),
                              const SizedBox(height: 10),
                              userVoteOrientation == VoteOrientation.Abstaining
                                  ? generateSelectedContainer(context,
                                      generateRowVotedAbstained(context))
                                  : generateRowVotedAbstained(context),
                              const SizedBox(height: 10),
                              userVoteOrientation == VoteOrientation.Against
                                  ? generateSelectedContainer(
                                      context, generateRowVotedAgainst(context))
                                  : generateRowVotedAgainst(context)
                            ])),
                  ],
                )),
            IconButton.filled(
                onPressed: () => {
                      if (proposal.votingResultInSpeciality != null)
                        {nextStep(proposal, userVoteOrientation)}
                      else
                        {nextStep()}
                    },
                icon: Icon(
                  Icons.arrow_circle_right_outlined,
                  color: baseTheme.colorScheme.primary,
                  size: 50,
                ))
          ]),
        ));
  }
}
