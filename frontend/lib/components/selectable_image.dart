import 'package:flutter/material.dart';
import 'package:frontend/themes/base_theme.dart';

class SelectableImage extends StatelessWidget {
  const SelectableImage({
    super.key,
    required this.isSelected,
    required this.imageAsset,
    required this.onTap,
  });

  final bool isSelected;
  final String imageAsset;
  final void Function(String imageAsset) onTap;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: InkWell(
        onTap: () => onTap(imageAsset),
        child: Container(
          decoration: BoxDecoration(
            border: Border.all(
              width: 3,
              color:
                  isSelected
                      ? baseTheme.colorScheme.primary
                      : Colors.transparent,
            ),
          ),
          child: Image.asset(imageAsset),
        ),
      ),
    );
  }
}
