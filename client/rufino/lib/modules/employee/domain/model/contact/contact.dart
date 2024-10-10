import 'package:rufino/modules/employee/domain/model/contact/cellphone.dart';
import 'package:rufino/modules/employee/domain/model/contact/email.dart';
import 'package:rufino/modules/employee/domain/model/enumeration.dart';
import 'package:rufino/modules/employee/domain/model/enumeration_collection.dart';
import 'package:rufino/modules/employee/domain/model/model_base.dart';
import 'package:rufino/modules/employee/domain/model/text_prop_base.dart';

class Contact extends ModelBase {
  final Cellphone cellphone;
  final Email email;

  static Contact get loadingContact =>
      Contact(Cellphone.empty, Email.empty, isLoading: true);

  const Contact(this.cellphone, this.email, {super.isLoading = false});

  factory Contact.fromJson(Map<String, dynamic> json) {
    return Contact(
        Cellphone.createFormatNumber(json["cellphone"]), Email(json["email"]),
        isLoading: false);
  }

  @override
  List<TextPropBase> get props => [cellphone, email];

  @override
  List<ModelBase> get models => [];

  @override
  List<EnumerationCollection> get enumerationCollection => [];

  @override
  List<List<Enumeration>> get enumerations => [];
}
