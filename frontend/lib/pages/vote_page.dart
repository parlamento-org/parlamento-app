import 'package:flutter/material.dart';
import 'package:frontend/constants/user_session.dart';
import 'package:frontend/controllers/vote_controller.dart';
import 'package:frontend/models/proposal.dart';
import 'package:flip_card/flip_card.dart';
import 'package:frontend/models/vote_model.dart';
import 'package:frontend/themes/base_theme.dart';

class VotePage extends StatefulWidget {
  const VotePage({super.key, required this.displayProposalResults});
  final Function(Proposal, VoteOrientation) displayProposalResults;

  @override
  State<VotePage> createState() => _VotePageState();
}

class _VotePageState extends State<VotePage> {
  VoteController voteController = VoteController();
  Proposal? proposal;
  bool _isLoading = true;

  void castUserVote(VoteOrientation votingOrientation) async {
    if (proposal == null) return;
    try {
      setState(() {
        _isLoading = true;
      });
      UserVote userVote = UserVote(
          userID: globalUserSession.userId,
          proposalID: proposal!.id,
          voteOrientation: votingOrientation);
      await voteController.castUserVote(userVote);
      if (votingOrientation != VoteOrientation.NotInterested) {
        widget.displayProposalResults(proposal!, votingOrientation);
      } else {
        getRandomProposal();
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Error casting your vote'),
        ),
      );
      setState(() {
        _isLoading = false;
      });
    }
  }

  void getRandomProposal() async {
    try {
      Proposal? proposal = await voteController
          .getRandomProposal(globalUserSession.proposalCriteria!);
      setState(() {
        _isLoading = false;
        this.proposal = proposal;
      });
    } catch (e) {
      print(e);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
            content: Text(
                'Error getting proposal, you may be offline or they might be a problem with your connection!'),
            duration: Duration(seconds: 2)),
      );
    }
  }

  @override
  initState() {
    super.initState();
    getRandomProposal();
  }

  @override
  Widget build(BuildContext context) {
    //return a box with a border with three buttons at the bottom
    return Container(
      color: baseTheme.colorScheme.background,
      padding: const EdgeInsets.only(
          top: 16.0, bottom: 16.0, left: 32.0, right: 32.0),
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Positioned.fill(
            top: 10,
            bottom: 100,
            child: Container(
              height: MediaQuery.of(context).size.height * 0.8,
              width: MediaQuery.of(context).size.width * 0.9,
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(18.0),
                border: Border.all(
                  color: baseTheme.colorScheme.primary,
                  width: 2.0,
                ),
              ),
              child: Center(
                child: _isLoading
                    ? const Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          CircularProgressIndicator(),
                          SizedBox(height: 10),
                          Text("Carregando a próxima proposta")
                        ],
                      )
                    : FlipCard(
                        direction: FlipDirection.HORIZONTAL,
                        front: Center(
                            child: Padding(
                          padding: const EdgeInsets.only(left: 20, right: 20),
                          child: Text(
                            proposal!.title,
                            style: const TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                        )),
                        back: Center(
                            child: SingleChildScrollView(
                          child: Text(
                            proposal!.censoredText,
                            style: const TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                        )),
                      ),
              ),
            ),
          ),
          Positioned.fill(
              top: 0,
              bottom: MediaQuery.of(context).size.height * 0.75,
              child: Center(
                  child: Container(
                width: MediaQuery.of(context).size.width * 0.4,
                decoration: BoxDecoration(
                  color: baseTheme.colorScheme.primary,
                  borderRadius: BorderRadius.circular(18.0),
                  border: Border.all(
                    color: baseTheme.colorScheme.primary,
                    width: 2.0,
                  ),
                ),
                child: const Center(
                    child: Padding(
                        padding: EdgeInsets.only(left: 50, right: 50),
                        child: Text(
                          "Proposta-Lei",
                          style: TextStyle(
                              color: Colors.white, fontWeight: FontWeight.bold),
                        ))),
              ))),
          Positioned.fill(
              bottom: -400.0,
              child: Center(
                  child: Wrap(
                spacing: 20,
                children: [
                  IconButton(
                    iconSize: 100,
                    onPressed: () {
                      castUserVote(VoteOrientation.InFavor);
                    },
                    icon: Image.asset(
                      'lib/images/voto_favor.png',
                    ),
                  ),
                  IconButton(
                    iconSize: 100,
                    onPressed: () {
                      castUserVote(VoteOrientation.Against);
                    },
                    icon: Image.asset(
                      'lib/images/voto_contra.png',
                    ),
                  ),
                  IconButton(
                    iconSize: 100,
                    onPressed: () {
                      castUserVote(VoteOrientation.Abstaining);
                    },
                    icon: Image.asset(
                      'lib/images/voto_abster.png',
                    ),
                  ),
                ],
              ))),
          const SizedBox(height: 10),
          Positioned.fill(
              bottom: -550.0,
              child: Center(
                  child: TextButton(
                      style: buttonStyle,
                      onPressed: () =>
                          castUserVote(VoteOrientation.NotInterested),
                      child: const Padding(
                          padding: EdgeInsets.only(
                              left: 50, right: 50, top: 5, bottom: 5),
                          child: Text("Não interessado",
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 20,
                              ))))))
        ],
      ),
    );
  }
}
