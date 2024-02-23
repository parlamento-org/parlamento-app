import 'package:flutter/material.dart';
import 'package:frontend/themes/base_theme.dart';

import '../models/political_party.dart';

class FilterMenu extends StatefulWidget {
  const FilterMenu(
      {super.key,
      required this.politicalParties,
      required this.onFilterMenuUpdate});
  final List<PoliticalParty> politicalParties;
  final Function(int, int, List<PoliticalParty>) onFilterMenuUpdate;
  @override
  State<FilterMenu> createState() => _MyFilterMenuState();
}

class _MyFilterMenuState extends State<FilterMenu> {
  RangeValues _currentRangeValues = const RangeValues(2000, 2023);

  @override
  Widget build(BuildContext context) {
    return Container(
        decoration: BoxDecoration(
          color: Colors.white.withOpacity(0.65),
          borderRadius: BorderRadius.circular(18.0),
          border: Border.all(
            color: baseTheme.colorScheme.primary,
            width: 2.0,
          ),
        ),
        child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [
          const SizedBox(height: 20),
          Padding(
              padding: EdgeInsets.only(left: 30, right: 30),
              child: RangeSlider(
                  values: _currentRangeValues,
                  min: 2000,
                  max: 2023,
                  divisions: 23,
                  labels: RangeLabels(
                    _currentRangeValues.start.round().toString(),
                    _currentRangeValues.end.round().toString(),
                  ),
                  onChanged: (RangeValues values) {
                    setState(() {
                      _currentRangeValues = values;
                    });
                  }))
        ]));
  }
}
