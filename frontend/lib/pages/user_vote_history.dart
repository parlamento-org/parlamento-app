import 'package:flutter/material.dart';
import 'package:frontend/components/filter_menu.dart';
import 'package:frontend/models/political_party.dart';
import 'package:frontend/themes/base_theme.dart';

import '../controllers/party_controller.dart';

class UserVoteHistory extends StatefulWidget {
  const UserVoteHistory({super.key});

  @override
  State<UserVoteHistory> createState() => _MyVoteHistoryState();
}

class _MyVoteHistoryState extends State<UserVoteHistory> {
  String _searchText = '';
  int earliestYear = 2000;
  int latestYear = 2023;
  List<PoliticalParty> allParties = [];
  List<PoliticalParty> excludedParties = [];
  bool _filterMenuIsOpen = false;

  void tryGetPoliticalParties() {
    print("Attempting to fetch political parties");
    PartyController().getPoliticalParties().then((parties) {
      setState(() {
        allParties = parties;
        print(allParties);
      });
    }).catchError((error) {
      print(error);
    });
  }

  @override
  void initState() {
    super.initState();
    tryGetPoliticalParties();
  }

  void _updateFilterMenu(
      int earliestYear, int latestYear, List<PoliticalParty> excludedParties) {
    setState(() {
      this.earliestYear = earliestYear;
      this.latestYear = latestYear;
      this.excludedParties = excludedParties;
    });
  }

  void _onSearchTextChanged(String text) {
    setState(() {
      _searchText = text;
    });
  }

  void toggleFilterMenu() {
    setState(() {
      _filterMenuIsOpen = !_filterMenuIsOpen;
    });
  }

  static final FocusNode searchBarFocus = FocusNode();
  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        const SizedBox(height: 20),
        Row(mainAxisAlignment: MainAxisAlignment.center, children: [
          Flexible(
              child: SearchBar(
            padding: MaterialStateProperty.all<EdgeInsets>(
                const EdgeInsets.only(left: 10, right: 10)),
            constraints: BoxConstraints(maxWidth: 500),
            onChanged: _onSearchTextChanged,
            backgroundColor: MaterialStateProperty.all<Color>(Colors.white),
            shape: MaterialStateProperty.all<RoundedRectangleBorder>(
                RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(10.0),
            )),
            hintText: 'Search...',
            leading: Icon(
              Icons.search,
              color: baseTheme.colorScheme.primary,
            ),
            focusNode: searchBarFocus,
          )),
          IconButton(
            hoverColor: Colors.transparent,
            onPressed: toggleFilterMenu,
            icon: Image.asset('lib/images/filter_logo.png',
                color: baseTheme.colorScheme.primary),
          ),
        ]),

        //filter menu
        _filterMenuIsOpen
            ? Padding(
                padding: const EdgeInsets.all(32.0),
                child: FilterMenu(
                  politicalParties: allParties,
                  onFilterMenuUpdate: _updateFilterMenu,
                ))
            : const SizedBox.shrink(),
      ],
    );
  }
}
