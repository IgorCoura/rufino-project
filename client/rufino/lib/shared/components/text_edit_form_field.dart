// import 'package:flutter/material.dart';
// import 'package:flutter/widgets.dart';
// import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';
// import 'package:shimmer/shimmer.dart';

// class TextEditFormField extends StatelessWidget {
//   final Function(String value) onChanged;
//   final bool isLoading;
//   final TextPropBase textProp;
//   const TextEditFormField({
//     super.key,
//     required this.onChanged,
//     required this.textProp,
//     this.isLoading = false,
//   });

//   @override
//   Widget build(BuildContext context) {
//     return isLoading == false
//         ? TextFormField(
//             onChanged: (name) => onChanged(name),
//             decoration: InputDecoration(
//               labelText: 'Nome do Funcionário',
//               border: const OutlineInputBorder(),
//               errorText: state.textfieldErrorMessage.isEmpty
//                   ? null
//                   : state.textfieldErrorMessage,
//             ),
//             validator: (value) {
//               if (value == null || value.isEmpty) {
//                 return 'Por favor, insira um nome.';
//               }
//               return null;
//             },
//           )
//         : Shimmer.fromColors(
//             baseColor: Theme.of(context).colorScheme.surface,
//             highlightColor: Theme.of(context).colorScheme.onSurface,
//             child: TextFormField(
//               enabled: false,
//               decoration: const InputDecoration(
//                 border: OutlineInputBorder(),
//               ),
//             ),
//           );
//   }
// }
