// import 'package:flutter/material.dart';
// import 'package:rufino/modules/employee/domain/model/enumeration.dart';
// import 'package:rufino/modules/employee/domain/model/enumeration_collection.dart';
// import 'package:rufino/modules/employee/domain/model/model_base.dart';
// import 'package:rufino/modules/employee/domain/model/personal_info/marital_status.dart';
// import 'package:rufino/modules/employee/domain/model/text_prop_base.dart';
// import 'package:shimmer/shimmer.dart';

// class FormComponent extends StatefulWidget {
//   final ModelBase modelBase;
//   final Function saveFunction;
//   final String formName;
//   final Function loadingFormData;
//   final bool isSavingData;
//   final Function(int id) removerItemFromList;
//   final Function addItemInList;
//   final Function(int? id) changeSelectedItem;
//   const FormComponent(
//       {required this.modelBase,
//       required this.saveFunction,
//       required this.formName,
//       required this.loadingFormData,
//       required this.isSavingData,
//       required this.removerItemFromList,
//       required this.addItemInList,
//       required this.changeSelectedItem,
//       super.key});

//   @override
//   State<FormComponent> createState() => _FormComponentState();
// }

// class _FormComponentState extends State<FormComponent> {
//   final _formKey = GlobalKey<FormState>();
//   bool _isExpanded = false;
//   bool _isEditing = false;

//   @override
//   Widget build(BuildContext context) {
//     return Container(
//       decoration: BoxDecoration(
//         border: Border.all(),
//         borderRadius: BorderRadius.circular(5),
//       ),
//       child: Column(
//         children: [
//           Container(
//             padding: const EdgeInsets.all(8),
//             decoration: BoxDecoration(
//               border: _isExpanded ? const Border(bottom: BorderSide()) : null,
//               borderRadius: BorderRadius.circular(5),
//             ),
//             child: InkWell(
//               onTap: () => {
//                 setState(() {
//                   _isExpanded = !_isExpanded;
//                   if (_isExpanded == false) {
//                     _isEditing = false;
//                   } else {
//                     widget.loadingFormData();
//                   }
//                 })
//               },
//               child: Row(
//                 mainAxisAlignment: MainAxisAlignment.spaceBetween,
//                 children: [
//                   Text(
//                     widget.formName,
//                     style: const TextStyle(
//                         fontWeight: FontWeight.bold, fontSize: 16),
//                   ),
//                   const Icon(
//                     Icons.arrow_drop_down_sharp,
//                   )
//                 ],
//               ),
//             ),
//           ),
//           _isExpanded
//               ? Padding(
//                   padding: const EdgeInsets.all(8.0),
//                   child: Form(
//                       key: _formKey,
//                       child: Column(
//                         children: [
//                           const SizedBox(
//                             height: 16,
//                           ),
//                           Column(
//                               children: widget.modelBase.props
//                                   .map((prop) => _textFormField(context, prop))
//                                   .toList()),
//                           widget.modelBase.props.isEmpty
//                               ? const SizedBox(
//                                   height: 16,
//                                 )
//                               : Container(),
//                           Column(
//                               children: widget.modelBase.enumerations
//                                   .map((listEnumeration) => _listViewOfItens(
//                                       context, listEnumeration))
//                                   .toList()),
//                           _viewEditEnumeration(
//                             context,
//                             EnumerationCollection(MaritalStatus.empty, [
//                               MaritalStatus.empty,
//                               MaritalStatus(1, "solteiro")
//                             ]),
//                           ),
//                           widget.modelBase.enumerations.isEmpty
//                               ? const SizedBox(
//                                   height: 16,
//                                 )
//                               : Container(),
//                           Align(
//                             alignment: Alignment.centerRight,
//                             child: widget.isSavingData
//                                 ? const CircularProgressIndicator()
//                                 : _isEditing
//                                     ? Row(
//                                         mainAxisAlignment:
//                                             MainAxisAlignment.end,
//                                         children: [
//                                           TextButton(
//                                             onPressed: () => {
//                                               setState(() {
//                                                 _isEditing = false;
//                                               })
//                                             },
//                                             child: const Text("Cancelar"),
//                                           ),
//                                           FilledButton(
//                                             onPressed: () {
//                                               if (_formKey.currentState !=
//                                                       null &&
//                                                   _formKey.currentState!
//                                                       .validate()) {
//                                                 _formKey.currentState!.save();
//                                                 setState(() {
//                                                   _isEditing = false;
//                                                 });
//                                                 widget.saveFunction();
//                                               }
//                                             },
//                                             child: const Text("Salvar"),
//                                           ),
//                                         ],
//                                       )
//                                     : TextButton(
//                                         onPressed: () {
//                                           setState(() {
//                                             _isEditing = true;
//                                           });
//                                         },
//                                         child: const Text("Editar"),
//                                       ),
//                           ),
//                         ],
//                       )),
//                 )
//               : Container(),
//         ],
//       ),
//     );
//   }

//   Widget _textFormField(BuildContext context, TextPropBase prop) {
//     return Column(
//       children: [
//         widget.modelBase.isLoading
//             ? Shimmer.fromColors(
//                 baseColor: Theme.of(context).colorScheme.surface,
//                 highlightColor: Theme.of(context).colorScheme.onSurface,
//                 child: TextFormField(
//                   enabled: false,
//                   decoration: const InputDecoration(
//                     border: OutlineInputBorder(),
//                   ),
//                 ),
//               )
//             : TextFormField(
//                 inputFormatters:
//                     prop.formatter != null ? [prop.formatter!] : null,
//                 keyboardType: prop.inputType,
//                 controller: TextEditingController(text: prop.value),
//                 enabled: _isEditing,
//                 decoration: InputDecoration(
//                     labelText: prop.displayName,
//                     border: const OutlineInputBorder()),
//                 style:
//                     TextStyle(color: Theme.of(context).colorScheme.onSurface),
//                 validator: (value) => prop.validate(value),
//                 onSaved: (newValue) {
//                   if (newValue != null) {
//                     prop.value = newValue;
//                   }
//                 },
//               ),
//         const SizedBox(
//           height: 16,
//         ),
//       ],
//     );
//   }

//   Widget _viewEditEnumeration(
//       BuildContext context, EnumerationCollection enumeration) {
//     return DropdownButtonFormField(
//       items: enumeration.list
//           .map((e) => DropdownMenuItem(value: e.id, child: Text(e.name)))
//           .toList(),
//       onChanged: _isEditing
//           ? (int? value) {
//               widget.changeSelectedItem(value);
//             }
//           : null,
//       decoration: InputDecoration(
//           enabled: _isEditing,
//           labelText: enumeration.selectedItem.displayName,
//           border: const OutlineInputBorder()),
//     );
//   }

//   Widget _listViewOfItens(BuildContext context, List<Enumeration> listItem) {
//     return Stack(
//       children: [
//         Padding(
//           padding: const EdgeInsets.only(top: 8),
//           child: Container(
//             padding: const EdgeInsets.all(8),
//             decoration: BoxDecoration(
//               border: Border.all(
//                   color: _isEditing
//                       ? Theme.of(context).colorScheme.primary
//                       : Theme.of(context).disabledColor),
//               borderRadius: BorderRadius.circular(5),
//             ),
//             child: Column(
//               children: [
//                 Column(
//                   children: listItem
//                       .map((x) => Column(
//                             children: [
//                               Container(
//                                 height: 42,
//                                 decoration: BoxDecoration(
//                                   border: Border(
//                                       bottom: BorderSide(
//                                           color: Theme.of(context)
//                                               .colorScheme
//                                               .onSurface)),
//                                 ),
//                                 padding:
//                                     const EdgeInsets.only(left: 8, right: 8),
//                                 child: Row(
//                                   mainAxisAlignment:
//                                       MainAxisAlignment.spaceBetween,
//                                   children: [
//                                     Text(x.name),
//                                     _isEditing
//                                         ? IconButton(
//                                             icon: Icon(Icons.delete,
//                                                 color: Theme.of(context)
//                                                     .colorScheme
//                                                     .error),
//                                             onPressed: () =>
//                                                 widget.removerItemFromList(
//                                                     x.id!), // TODO: Remove !
//                                           )
//                                         : Container(),
//                                   ],
//                                 ),
//                               )
//                             ],
//                           ))
//                       .toList(),
//                 ),
//                 const SizedBox(
//                   height: 8,
//                 ),
//                 _isEditing
//                     ? Align(
//                         alignment: Alignment.centerRight,
//                         child: TextButton(
//                           onPressed: () => widget.addItemInList,
//                           child: Text(
//                             "Adicionar ${listItem.first.displayName}",
//                             style: const TextStyle(fontSize: 14),
//                           ),
//                         ))
//                     : Container(),
//                 const SizedBox(
//                   height: 8,
//                 ),
//               ],
//             ),
//           ),
//         ),
//         Positioned(
//           left: 10,
//           top: -2,
//           child: Container(
//             color: Colors.white, // Fundo branco para cobrir a borda
//             padding: const EdgeInsets.symmetric(horizontal: 5),
//             child: Text(
//               listItem.first.displayName,
//               style: TextStyle(
//                   color: _isEditing
//                       ? Theme.of(context).colorScheme.primary
//                       : Theme.of(context).disabledColor,
//                   fontSize: 12),
//             ),
//           ),
//         ),
//       ],
//     );
//   }
// }
