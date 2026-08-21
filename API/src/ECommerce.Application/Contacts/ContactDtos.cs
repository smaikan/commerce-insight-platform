using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Contacts;

public sealed record ContactSubmissionReceiptDto(string ReferenceNumber, DateTime SubmittedAt);

public sealed record ContactMessageSummaryDto(
    Guid Id,
    string ReferenceNumber,
    string Name,
    string Email,
    ContactMessageSubject Subject,
    ContactMessageStatus Status,
    string? ProvidedOrderNumber,
    bool HasVerifiedOrder,
    string? AssignedAdminUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record ContactMessageActivityDto(
    Guid Id,
    ContactMessageActivityType Type,
    string? ActorAdminUserId,
    string? Content,
    string? PreviousValue,
    string? NewValue,
    Guid? ReplyId,
    DateTime CreatedAt);

public sealed record ContactMessageReplyDto(
    Guid Id,
    string AdminUserId,
    string Body,
    ContactReplyDeliveryStatus DeliveryStatus,
    DateTime CreatedAt);

public sealed record ContactMessageDetailDto(
    Guid Id,
    string ReferenceNumber,
    string? UserId,
    string Name,
    string Email,
    string? Phone,
    ContactMessageSubject Subject,
    string? ProvidedOrderNumber,
    Guid? VerifiedOrderId,
    bool IsOrderVerified,
    string Message,
    ContactMessageStatus Status,
    string? AssignedAdminUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? FirstRespondedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    Guid ConcurrencyToken,
    string PrivacyNoticeVersion,
    DateTime PrivacyNoticePublishedAt,
    IReadOnlyList<ContactMessageActivityDto> Activities,
    IReadOnlyList<ContactMessageReplyDto> Replies);

public static class ContactDtoMapping
{
    // Burada liste projection'ını tam mesaj ve telefon taşımadan güvenli özet DTO'ya dönüştürüyorum.
    public static ContactMessageSummaryDto ToSummaryDto(this ContactMessage message) =>
        new(
            message.Id,
            message.ReferenceNumber,
            message.Name,
            message.Email,
            message.Subject,
            message.Status,
            message.ProvidedOrderNumber,
            message.VerifiedOrderId.HasValue,
            EncodeOptionalUserId(message.AssignedAdminUserId),
            message.CreatedAt,
            message.UpdatedAt);

    // Burada yönetim detay grafiğini public kullanıcı kimlikleri ve teslimat görünümüyle DTO'ya dönüştürüyorum.
    public static ContactMessageDetailDto ToDetailDto(this ContactMessage message) =>
        new(
            message.Id,
            message.ReferenceNumber,
            EncodeOptionalUserId(message.UserId),
            message.Name,
            message.Email,
            message.Phone,
            message.Subject,
            message.ProvidedOrderNumber,
            message.VerifiedOrderId,
            message.VerifiedOrderId.HasValue,
            message.Message,
            message.Status,
            EncodeOptionalUserId(message.AssignedAdminUserId),
            message.CreatedAt,
            message.UpdatedAt,
            message.FirstRespondedAt,
            message.ResolvedAt,
            message.ClosedAt,
            message.ConcurrencyToken,
            message.PrivacyNoticeVersion,
            message.PrivacyNoticePublishedAt,
            message.Activities.OrderBy(activity => activity.CreatedAt).ThenBy(activity => activity.Id).Select(ToActivityDto).ToList(),
            message.Replies.OrderBy(reply => reply.CreatedAt).ThenBy(reply => reply.Id).Select(ToReplyDto).ToList());

    // Burada activity actor kimliğini public kullanıcı kimliğine çeviriyorum.
    private static ContactMessageActivityDto ToActivityDto(ContactMessageActivity activity) =>
        new(
            activity.Id,
            activity.Type,
            EncodeOptionalUserId(activity.ActorAdminUserId),
            activity.Content,
            MapActivityValue(activity.Type, activity.PreviousValue),
            MapActivityValue(activity.Type, activity.NewValue),
            activity.ReplyId,
            activity.CreatedAt);

    // Burada assignment activity iç kimliklerini API'ye açmadan public kullanıcı kimliğine dönüştürüyorum.
    private static string? MapActivityValue(ContactMessageActivityType type, string? value)
    {
        if (type != ContactMessageActivityType.AssignmentChanged || string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return long.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var id) && id > 0
            ? PublicIdCodec.EncodeUserId(id)
            : null;
    }

    // Burada reply outbox durumundan SMTP teslimat görünümünü türetiyorum.
    private static ContactMessageReplyDto ToReplyDto(ContactMessageReply reply) =>
        new(
            reply.Id,
            PublicIdCodec.EncodeUserId(reply.AdminUserId),
            reply.Body,
            GetDeliveryStatus(reply.OutboxMessage),
            reply.CreatedAt);

    // Burada outbox lease ve terminal alanlarını yönetim teslimat enumuna dönüştürüyorum.
    private static ContactReplyDeliveryStatus GetDeliveryStatus(EmailOutboxMessage outbox) =>
        outbox.ProcessedAt.HasValue
            ? ContactReplyDeliveryStatus.Sent
            : outbox.DeadLetteredAt.HasValue
                ? ContactReplyDeliveryStatus.DeadLetter
                : outbox.AttemptCount > 0
                    ? ContactReplyDeliveryStatus.Retrying
                    : ContactReplyDeliveryStatus.Queued;

    // Burada nullable iç kullanıcı kimliğini yalnız API sınırında public forma çeviriyorum.
    private static string? EncodeOptionalUserId(long? id) => id.HasValue ? PublicIdCodec.EncodeUserId(id.Value) : null;
}
