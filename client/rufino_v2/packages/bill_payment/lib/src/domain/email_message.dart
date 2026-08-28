/// The captured e-mail as the viewer shows it: header plus renderable body.
class EmailMessage {
  /// Creates the e-mail record.
  const EmailMessage({
    required this.id,
    required this.sender,
    required this.receivedAt,
    required this.contentType,
    required this.content,
    this.subject,
  });

  /// The captured message's id.
  final String id;

  /// The sender's address, normalized.
  final String sender;

  /// The subject, when the e-mail carried one.
  final String? subject;

  /// When the e-mail reached the mailbox.
  final DateTime receivedAt;

  /// `text/html` or `text/plain`, as the provider declared it.
  final String contentType;

  /// The raw body — HTML or plain text, per [contentType].
  final String content;

  /// Whether the body should go through the HTML renderer.
  bool get isHtml => contentType.toLowerCase().contains('html');
}
