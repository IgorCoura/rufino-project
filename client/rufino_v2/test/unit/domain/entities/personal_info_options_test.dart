import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/personal_info_options.dart';
import 'package:rufino_v2/domain/entities/selection_option.dart';

void main() {
  const populated = [
    SelectionOption(id: '1', name: 'Option A'),
    SelectionOption(id: '2', name: 'Option B'),
  ];

  const empty = <SelectionOption>[];

  group('PersonalInfoOptions.isLoaded', () {
    test('returns true when all lists are populated', () {
      const options = PersonalInfoOptions(
        genders: populated,
        maritalStatuses: populated,
        ethnicities: populated,
        educationLevels: populated,
        disabilities: populated,
      );
      expect(options.isLoaded, isTrue);
    });

    test('returns false when any list is empty', () {
      const options = PersonalInfoOptions(
        genders: populated,
        maritalStatuses: empty,
        ethnicities: populated,
        educationLevels: populated,
        disabilities: populated,
      );
      expect(options.isLoaded, isFalse);
    });

    test('returns false when all lists are empty', () {
      const options = PersonalInfoOptions(
        genders: empty,
        maritalStatuses: empty,
        ethnicities: empty,
        educationLevels: empty,
        disabilities: empty,
      );
      expect(options.isLoaded, isFalse);
    });
  });

  group('PersonalInfoOptions label delegates', () {
    const genders = [SelectionOption(id: '1', name: 'Masculino')];
    const marital = [SelectionOption(id: '2', name: 'Casado')];
    const ethnic = [SelectionOption(id: '3', name: 'Pardo')];
    const education = [SelectionOption(id: '4', name: 'Superior')];
    const disability = [SelectionOption(id: '5', name: 'Nenhuma')];

    const options = PersonalInfoOptions(
      genders: genders,
      maritalStatuses: marital,
      ethnicities: ethnic,
      educationLevels: education,
      disabilities: disability,
    );

    test('genderLabel returns the matching gender name', () {
      expect(options.genderLabel('1'), 'Masculino');
    });

    test('genderLabel returns Não informado for unknown id', () {
      expect(options.genderLabel('99'), 'Não informado');
    });

    test('maritalStatusLabel returns the matching marital status name', () {
      expect(options.maritalStatusLabel('2'), 'Casado');
    });

    test('ethnicityLabel returns the matching ethnicity name', () {
      expect(options.ethnicityLabel('3'), 'Pardo');
    });

    test('educationLevelLabel returns the matching education level name', () {
      expect(options.educationLevelLabel('4'), 'Superior');
    });

    test('disabilityLabel returns the matching disability name', () {
      expect(options.disabilityLabel('5'), 'Nenhuma');
    });
  });
}
