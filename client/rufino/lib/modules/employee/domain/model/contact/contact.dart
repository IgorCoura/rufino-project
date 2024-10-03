import 'package:rufino/modules/employee/domain/model/contact/cellphone.dart';
import 'package:rufino/modules/employee/domain/model/contact/email.dart';
import 'package:rufino/modules/employee/domain/model/model_base.dart';
import 'package:rufino/modules/employee/domain/model/prop_base.dart';

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
  List<PropBase> get props => [cellphone, email];

  @override
  List<ModelBase> get models => [];
}
