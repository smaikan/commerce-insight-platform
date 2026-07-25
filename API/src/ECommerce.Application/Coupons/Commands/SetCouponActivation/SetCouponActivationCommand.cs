using ECommerce.Application.Coupons.Dtos;
using MediatR;

namespace ECommerce.Application.Coupons.Commands.SetCouponActivation;

// Burada yÃ¶neticinin kuponu kullanÄ±ma aÃ§ma veya kapatma isteÄŸini taÅŸÄ±yorum.
public sealed record SetCouponActivationCommand(Guid Id, bool IsActive) : IRequest<CouponDto>;
