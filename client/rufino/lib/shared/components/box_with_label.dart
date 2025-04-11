import 'package:flutter/material.dart';

class BoxWithLabel extends StatelessWidget {
  final Widget child;
  final String label;
  const BoxWithLabel({super.key, required this.label, required this.child});

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        Padding(
          padding: const EdgeInsets.only(top: 8),
          child: Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                border:
                    Border.all(color: Theme.of(context).colorScheme.primary),
                borderRadius: BorderRadius.circular(5),
              ),
              child: child),
        ),
        _labelText(context)
      ],
    );
  }

  Widget _labelText(BuildContext context) {
    return Positioned(
      left: 10,
      top: -2,
      child: Container(
        color: Theme.of(context).colorScheme.surface,
        padding: const EdgeInsets.symmetric(horizontal: 5),
        child: Text(
          label,
          style: TextStyle(
              color: Theme.of(context).colorScheme.primary, fontSize: 12),
        ),
      ),
    );
  }
}
