# Phase 4 — Internal in-app notifications

Phase 4 adds database-backed **internal ERP notifications**. They are for authenticated employees only. Customers do not receive messages. WhatsApp, SMS, email, and push are intentionally not implemented.

## Architecture

Business services (`OrderService`, `ModificationService`, `DispatchService`) complete the domain change, then call `INotificationService` **in the same EF transaction** before `SaveChanges`. If the business operation rolls back, the notifications roll back with it.

Recipient resolution uses `IUserDirectory` (Infrastructure `IdentityUserDirectory` + ASP.NET Identity roles). Application code never hard-codes user IDs.

Inbox queries always filter `RecipientUserId` to the authenticated user. The existing `ICompanyScoped` global query filter remains as defense in depth. SuperAdmin still only sees notifications **addressed to them**, which includes copies of company events across all companies.

## Entity

`Notification` (existing table, extended):

| Field | Notes |
| --- | --- |
| Id | GUID |
| CompanyId | Company of the related business event |
| RecipientUserId | Mapped to existing `UserId` column |
| Type | `NotificationType` enum stored as int |
| Title / Message | Display copy |
| RelatedEntityType / RelatedEntityId | `Order` or `OrderModificationRequest` |
| EventKey | Idempotency key `{type}:{relatedId}:{recipientId}` |
| IsRead / ReadAt | Read state; `ReadAt` is UTC |
| CreatedAt | UTC |

## Notification types

- `OrderCreated`
- `OrderReadyToDispatch`
- `OrderDispatched`
- `ModificationRequested`
- `ModificationApproved`
- `ModificationRejected`

## Recipient rules

| Event | Recipients | Excluded |
| --- | --- | --- |
| Order created | CompanyAdmin (that company), SuperAdmin | Sales employee who created the order |
| Ready to Dispatch | CompanyAdmin, DispatchUser(s) of that company, SuperAdmin | — |
| Dispatch Done | Order creator, CompanyAdmin, SuperAdmin | Dispatch user who completed dispatch |
| Modification requested | CompanyAdmin, SuperAdmin | Requester |
| Modification approved | Sales employee who requested it | Reviewer |
| Modification rejected | Sales employee who requested it (includes reason) | Reviewer |

Duplicate role membership (for example SuperAdmin also listed as CompanyAdmin) creates **one** row per user. Retrying the same successful event is skipped via `EventKey`.

## Security and company isolation

- All notification endpoints require authentication (`AuthenticatedUser`).
- Inbox, unread count, details, mark-as-read, and mark-all-as-read are **recipient-scoped** on the server.
- Guessing another user's notification ID returns **404**, not the record.
- CompanyAdmin cannot read another company's notifications.
- SalesEmployee and DispatchUser only see rows addressed to them.
- Mark-as-read and mark-all-as-read are **POST** with anti-forgery tokens. GET never mutates read state.
- Notification read/unread is not audit-logged.

## Read / unread

Unread: `IsRead = false`.

A notification is marked read when the user clicks it (POST `Open` or `MarkAsRead`) or uses **Mark all as read**. Rendering the bell dropdown does **not** mark items read.

## UI

- Bell in the existing top bar (CSS bell icon, numeric badge hidden at 0)
- Dropdown shows the latest 8 notifications and a **View all** link
- `/Notifications` paginated newest-first list
- Click opens `/Orders/Details/{id}` or `/ModificationRequests/Details/{id}` when the related row still exists
- Lightweight poll of `/Notifications/UnreadCount` every 45 seconds (no SignalR)

## Database

Migration: `20260915171410_Phase4Notifications`

- `Type`, `RelatedEntityType`, `RelatedEntityId`, `EventKey`
- `UserId` required (recipient)
- Indexes: `(UserId, IsRead, CreatedAt)` and unique `EventKey`

SQLite development databases created before this migration are patched by `SqliteSchemaPatcher`.

## Testing

Automated coverage includes creation, recipients, duplicate suppression, pagination, newest-first, unread count, mark read / mark all, unauthorized and cross-company access, workflow events, and SQLite transaction rollback.

## Future channel boundary

Phase 5+ may add WhatsApp/SMS/email **without rewriting** this inbox. External delivery should consume the same business events (or this stored notification data) as a separate channel. This phase does not add fake WhatsApp/SMS services.
