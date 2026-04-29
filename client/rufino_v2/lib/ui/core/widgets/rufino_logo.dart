import 'package:flutter/widgets.dart';
import 'package:flutter_svg/flutter_svg.dart';

/// The Rufino brand badge — a teal rounded square with a white "R" monogram.
///
/// Renders the bundled SVG at the requested [size]. Override [semanticLabel]
/// when the logo plays a role beyond pure decoration so screen readers
/// describe it accurately.
class RufinoLogo extends StatelessWidget {
  const RufinoLogo({
    super.key,
    this.size = 96,
    this.semanticLabel = 'Rufino',
  });

  final double size;
  final String semanticLabel;

  @override
  Widget build(BuildContext context) {
    return SvgPicture.asset(
      'assets/img/rufino-logo.svg',
      width: size,
      height: size,
      semanticsLabel: semanticLabel,
    );
  }
}
